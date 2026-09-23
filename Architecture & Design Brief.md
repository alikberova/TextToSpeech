# Background Processing Refactor - Architecture & Design Brief

## 1. Goal

Redesign the application's background-processing architecture for long-term extensibility and reliability.

The goal is not simply to replace the current in-process queue with RabbitMQ.

The target is a reusable background-job platform inside the application that can initially execute text-to-speech generation, but can later support other asynchronous operations without redesigning the fundamental architecture.

Examples may include:

- SpeechGeneration
- DocumentProcessing
- BookAnalysis
- AudioProcessing
- ImageProcessing
- Notifications
- future job types

Backward compatibility with the existing background-processing implementation is not required.

There is no production deployment or production database that must be preserved. Prefer the cleaner long-term architecture even when it requires substantial restructuring.

The backend must also be designed for future:

- Web
- Android
- iOS

clients.

## 2. Target architecture

```text
Web / Android / iOS
        |
        v
      API
        |
        | application transaction
        v
   PostgreSQL
   ├── application schema(s)
   └── jobs schema
        |
        | transactional outbox
        v
   Outbox Publisher
        |
        v
     RabbitMQ
        |
        v
    Worker(s)
        |
        v
   Job Handler
        |
        +--> External provider
        |
        +--> PostgreSQL
        |
        +--> Object Storage
```

The main architectural rule is:

```text
PostgreSQL    = authoritative job lifecycle state
RabbitMQ      = work delivery / transport
Worker memory = temporary execution state
Redis         = cache only, if still justified
SignalR       = real-time notification only
Object storage = large source/result artifacts
```

No other component should become an alternative source of truth for job state.

## 3. API and Worker are separate services

### API

The API is a separate project/process/deployable service.

It is responsible for:

- authentication and authorization;
- request validation;
- application use cases;
- creating background operations;
- querying job state;
- requesting cancellation;
- exposing completed results;
- real-time notifications to connected clients;
- future mobile-friendly APIs.

The API must not execute long-running work.

Controllers should not directly manipulate job persistence.

Preferred conceptual flow:

```text
Controller
    |
    v
Application Use Case
    |
    +--> domain changes
    |
    +--> background-job abstraction
```

For example:

```text
POST speech request
        |
        v
SubmitSpeechGeneration
        |
        +--> create durable speech request
        +--> create BackgroundJob
        +--> create OutboxMessage
```

### Worker

The Worker must be a separate .NET project, process, container, and deployable service.

It is responsible for:

- consuming work from RabbitMQ;
- acquiring execution ownership;
- executing job handlers;
- updating progress;
- managing execution attempts;
- handling cancellation during execution;
- retrying when appropriate;
- recovering abandoned executions;
- storing results;
- recording lifecycle history.

The Worker should be stateless.

It must be possible to move from:

```text
Worker x1
```

to:

```text
Worker xN
```

without redesigning the application.

Worker code must not depend on the API project.

## 4. PostgreSQL and module boundaries

Use one PostgreSQL database initially.

Separate concerns logically:

```text
application.*
jobs.*
```

The jobs subsystem should have a strong internal module/persistence boundary so it can later be extracted into a separate database or Job Service without forcing unrelated application code to change.

The architecture should not depend on application code accessing job tables directly.

The jobs module should own its persistence API.

## 5. Generic Background Job model

Do not build the infrastructure around SpeechJob.

The core lifecycle concept should be generic:

BackgroundJob

It represents execution, not the meaning of a particular operation.

Conceptually it should support:

- job ID;
- job type;
- lifecycle status;
- creation/start/completion timestamps;
- current progress;
- cancellation request;
- retry/attempt state;
- failure information;
- execution ownership/lease;
- idempotency information;
- correlation/tracing information;
- job definition/message version where required.

Exact fields and state transitions should be determined during detailed design.

## 6. Job-specific data must remain separate

The generic jobs subsystem must not know what a TTS provider, voice, model, document, etc. means.

Conceptually:

```text
BackgroundJob
    = generic execution lifecycle
```

```text
SpeechGenerationRequest
    = immutable speech-specific input
```

The relationship may be implemented through IDs/references, but the generic job model must not become a large table containing fields for every possible job type.

Future example:

```text
BackgroundJob
        |
        +--> SpeechGenerationRequest
```

```text
BackgroundJob
        |
        +--> BookAnalysisRequest
```

Adding a new job type should primarily require:

```text
new job-specific model
+
new job handler
```

rather than redesigning the job infrastructure.

## 7. Job input must be durable

A job must never depend on data captured only in process memory.

The original implementation captured extracted text inside an in-memory queued delegate. Part 3 replaces that with persisted requests and identifier-only dispatch.

Job execution input must therefore be durable before dispatch.

For speech generation, the preferred long-term direction is:

```text
uploaded source
      |
      v
durable storage
      |
      v
SpeechGenerationRequest
      |
      v
BackgroundJob
```

The Worker should be able to restart and reconstruct everything required to execute the job from durable state.

Job-specific input should represent an immutable snapshot of the requested operation, or reference immutable/versioned source data.

## 8. Large files and generated artifacts

Do not design PostgreSQL as the long-term storage for large binary artifacts.

Introduce a storage abstraction with object storage as the intended architecture for:

- uploaded documents/books;
- generated audio;
- other large future artifacts.

PostgreSQL should primarily store:

- metadata;
- ownership;
- object references;
- lifecycle state.

This will also support future mobile clients, potentially large uploads/downloads, and future CDN/direct-upload mechanisms without redesigning the job subsystem.

The exact object-storage provider is not an architectural decision for this brief.

## 9. BackgroundJobAttempt

Implement execution attempts as a separate durable concept from the beginning.

Conceptually:

```text
BackgroundJob
    |
    +-- Attempt 1
    +-- Attempt 2
    +-- Attempt 3
```

A job retry is not the same thing as RabbitMQ message redelivery.

### Message redelivery

Example:

- Worker receives message
- Worker crashes
- RabbitMQ redelivers the same message

### Job retry

Example:

```text
Attempt 1
    |
provider transient failure
    |
retry policy
    |
Attempt 2
```

These must have separate semantics.

## 10. BackgroundJobEvent

Implement BackgroundJobEvent from the beginning as an append-only lifecycle/history record.

It is not event sourcing and is not the authoritative current state.

BackgroundJob remains the current state.

BackgroundJobEvent provides historical information such as:

- Created
- Dispatched
- Started
- AttemptStarted
- RetryScheduled
- CancelRequested
- Cancelled
- Completed
- Failed
- Recovered

Do not write every small progress change as an event.

Progress can be persisted on the current job state at an appropriate frequency.

The event model should remain small and suitable for:

- debugging distributed execution;
- tracing retries and recovery;
- support investigations;
- cancellation history;
- future audit/history requirements;
- possible future user-visible activity history.

A retention strategy should be considered so history does not grow indefinitely.

## 11. Transactional Outbox

Creating a job and publishing directly to RabbitMQ must not be treated as one reliable operation.

Use the transactional outbox pattern:

```text
BEGIN DATABASE TRANSACTION
```

- application changes
- BackgroundJob
- OutboxMessage

```text
COMMIT
```

Then:

```text
Outbox Publisher
    |
    v
RabbitMQ
```

This creates a reliable DB-to-broker boundary.

## 12. At-least-once delivery is an explicit design assumption

Do not design around exactly-once message delivery.

Assume that duplicate messages can occur.

For example:

- publisher sends message
- RabbitMQ accepts it
- publisher crashes before recording success
- publisher retries
- same JobId is published again

Therefore:

Duplicate delivery is expected behavior, not an exceptional edge case.

Worker execution must be protected using durable state, execution ownership, leases, and idempotency mechanisms.

Concurrent equivalent submissions can both miss the completed-audio lookup and create separate operations. The Jobs module design must define a durable submission idempotency key, its scope, and an atomic transaction boundary for accepting the request and creating the job/outbox records. Returning only the audio ID, adding a lookup index, or restoring Redis caching does not prevent this race.

## 13. Worker ownership and recovery

Only one Worker should actively own execution of a given job attempt.

Use a durable ownership/lease mechanism or an equivalent concurrency mechanism.

The system must support:

- Worker A starts job
- Worker A crashes
- lease expires
- Worker B safely recovers job

The design must prevent two active Workers from blindly executing the same operation.

The exact locking/lease implementation should be determined during detailed design.

## 14. Provider-side idempotency is a separate concern

Generic job ownership alone does not guarantee that an external operation is safe to repeat.

Example:

- Worker calls TTS provider
- provider finishes/bills operation
- Worker crashes before result is persisted

- job is recovered
- new Worker calls provider again

This can cause duplicate external work and duplicate cost.

This must be treated as a first-class architectural concern.

For each external provider, investigate:

- idempotency-key support;
- operation IDs;
- ability to query previously submitted work;
- ability to recover already-produced results;
- behavior after ambiguous network failures.

Where a provider cannot guarantee idempotency, the job handler must explicitly define how checkpointing, persisted intermediate state, retries, and duplicate-cost risk are handled.

Do not assume that a generic deterministic ID automatically solves this.

## 15. Cancellation semantics

Separate:

CancellationRequested

from:

Cancelled

Example:

```text
RUNNING
   |
user requests cancellation
   |
RUNNING + cancellation requested
   |
Worker observes request
   |
CANCELLED
```

Cancellation intent must be durable.

The design must support:

- cancellation before execution begins;
- cancellation while execution is running;
- cancellation while the Worker is temporarily unavailable;
- authorization so users can cancel only operations they own.

## 16. RabbitMQ responsibility

RabbitMQ is the dispatch/transport layer.

It should normally carry small messages, such as:

- JobId
- message/contract version
- correlation information

It should not become the database for complete job state.

RabbitMQ messages must not contain audio bytes (including Base64-encoded audio) or serialized storage entities such as AudioFile. The message contract must remain unchanged when audio storage moves from PostgreSQL to object storage.

The Worker should use JobId to load the durable speech request, including FileId, OwnerId, provider, generation options, and source references. FileId is a logical artifact identifier, independent of its physical storage location. The Worker reads and persists artifact content through the storage/persistence abstraction described in section 8; the RabbitMQ contract does not describe how audio is stored. The current in-memory SpeechGenerationInput is transitional execution input, not the future RabbitMQ message contract.

RabbitMQ topology, ACK/NACK behavior, dead-lettering, delayed retries, and delivery policy should be designed after the generic lifecycle is defined.

## 17. Client architecture - Web, Android, iOS

All clients communicate with the API.

```text
Web ───────┐
Android ───┼──> API
iOS ───────┘
```

Clients must not know anything about:

- RabbitMQ;
- Workers;
- job leases;
- PostgreSQL;
- internal retries.

The API should provide capabilities for:

- submit operation
- get durable status
- request cancellation
- obtain result

Exact endpoint design should be determined later.

## 18. SignalR and future mobile notifications

SignalR may provide real-time status/progress for Web and active clients.

However:

SignalR is a notification mechanism, not job state.

A client that disconnects, refreshes, restarts, or resumes after several hours must recover state through the API.

This is especially important for Android/iOS because mobile applications may be suspended or terminated.

The architecture should allow future integration with:

- FCM
- APNs

for completion/failure notifications without changing the job execution architecture.

Do not implement push notifications in the first refactoring parts unless required.

## 19. Redis

Redis must not automatically be preserved merely because the current application uses it.

Evaluate its role during the refactor.

If retained:

```text
Redis = cache / performance optimization
```

It must not be required to reconstruct job state or determine whether a job completed.

The system must remain correct after Redis data is lost.

## 20. Scheduling

Do not build a general scheduled-job subsystem at this stage unless an actual application requirement exists.

Future-proofing should come from correct boundaries rather than implementing every hypothetical feature.

The generic architecture must make scheduling addable later without redesigning job execution.

Internal delayed retry requirements should be designed separately from future user-facing scheduled jobs.

## 21. Job framework decision checkpoint

Before implementing the durable job subsystem, explicitly confirm the choice of job engine.

The current architectural direction assumes:

```text
application-owned BackgroundJob lifecycle
+
PostgreSQL
+
transactional outbox
+
RabbitMQ
+
custom Worker
```

Before committing to implementation, compare this deliberately against adopting an existing framework/platform such as Hangfire or Temporal.

The goal is not to add another framework automatically.

The goal is to avoid building a large custom job engine if an existing solution provides the required semantics more cleanly.

If RabbitMQ remains the primary job transport, do not combine RabbitMQ and another job engine in overlapping roles without a clear ownership model.

This decision should be made once, explicitly, before Part 2.

Decision approved: use the custom Jobs module with PostgreSQL as the lifecycle authority,
a transactional outbox, RabbitMQ transport and a separate Worker. Hangfire and Temporal
were considered; their own execution/storage models would overlap with the explicitly
required Jobs lifecycle and dispatch model.

## 22. Speech generation is the first job type

Speech generation becomes the first implementation of the generic architecture.

The current speech flow mixes several responsibilities:

- HTTP submission
- file reading
- text extraction
- deduplication/cache
- in-memory queue
- cancellation
- provider execution
- progress
- persistence
- SignalR

The refactor should separate these responsibilities rather than wrapping the existing SpeechService in RabbitMQ.

## 23. Initial implementation strategy

### Part 1 - Refactor application and execution boundaries

First inspect the current dev branch and trace the complete full speech-generation lifecycle.

Separate at least these conceptual operations:

SubmitSpeechGeneration

and:

ExecuteSpeechGeneration

They are different application operations.

The API submission path should own request acceptance and durable creation of the requested operation.

The Worker execution path should eventually own the actual long-running processing.

Create clean reusable domain/application services underneath them where appropriate.

Do not create one universal "speech use case" that both API and Worker invoke merely to reduce duplication.

The existing in-memory queue does not need to be preserved as a long-term abstraction.

### Part 2 - Design and implement the generic Jobs module

Introduce the jobs module/schema and durable lifecycle.

At minimum, design the roles of:

- BackgroundJob
- BackgroundJobAttempt
- BackgroundJobEvent
- OutboxMessage

Also define:

- lifecycle states and transitions;
- cancellation semantics;
- ownership/leases;
- idempotency;
- correlation;
- retry semantics;
- history retention.

Speech generation becomes the first job type, but the generic jobs module must remain domain-agnostic.

Before implementation, complete the job-framework decision checkpoint described above.

#### Part 2 implementation boundary

- Public contracts live in `Core/Jobs`; persistence lives in `Infra/Jobs`, in the `jobs` schema.
  Domain code uses `ISubmitBackgroundJob` for acceptance and `IBackgroundJobs` for lifecycle
  operations, not the job tables.
- Submission is unique by owner, job type and idempotency key. The same key returns the
  original job/input reference; a different input fingerprint or input version is rejected.
  Owner ID and job type are limited to 100 characters each, and the idempotency key to 200,
  in both validation and schema. Their combined worst-case UTF-8 size stays below the
  B-tree entry limit for the standard PostgreSQL page size.
  The speech use case must derive the fingerprint from all generation-relevant input,
  including provider and options. Completed-audio reuse remains a separate domain decision.
- PostgreSQL serializes acceptance for each key. Job, initial history and dispatch outbox
  are saved together. If a caller opens an `AppDbContext` transaction, Jobs joins it.
  The caller must save immutable domain input only for a newly accepted job, then commit
  that same transaction. On failure, roll back and discard the context. Likewise, saving
  result metadata and completing a job must share a transaction. `CompleteAsync` rejects
  calls without an active caller transaction; a rejected completion
  must roll back the caller's result changes. External storage writes cannot participate
  in that database transaction and need their own reconciliation in Part 3.
- A pending job can be claimed once. Each claim creates an attempt with a fresh ID and
  expiring lease. Renewal, progress and terminal writes require that current, unexpired
  attempt. Lease decisions use PostgreSQL `clock_timestamp()` read after acquiring the
  job lock, so Worker clocks and the caller transaction's start time do not decide ownership.
  Duplicate deliveries do not create retries. The Worker checks input version
  support before claiming; unsupported versions require a compatible Worker deployment.
- Cancellation persists owner-authorized intent. Pending work can be cancelled immediately;
  running work requires Worker acknowledgement. A successfully persisted result may still
  complete after cancellation was requested. The existing SignalR behavior is unchanged.
- An expired lease marks the attempt abandoned and the job `RecoveryRequired`, including
  when cancellation was requested. Recovery never automatically repeats a provider call.
  A domain-aware reconciler must establish the external outcome before resolving recovery.
  Explicitly safe retries are bounded by `MaxAttempts` and create a new outbox record with
  `AvailableAt`; they are not user-facing scheduled jobs.
- History records lifecycle changes, not individual progress updates. Initially retain
  jobs/history and their idempotency keys; do not delete active or unresolved jobs.
  Automated retention is deferred until a retention period and idempotency window are
  agreed. Later cleanup must retain deduplication keys for that window and remove only
  terminal jobs whose dispatch records have all been published.
- The dispatch contract contains only `JobId`, `ContractVersion` and `CorrelationId`.
  Input/result IDs are logical references, independent of PostgreSQL versus object storage.
- This step supplies persistence and lifecycle operations. Source/result storage, outbox
  publishing, recovery polling, Worker execution and API speech integration remain in the
  subsequent parts. The existing in-process speech path is not switched to Jobs yet.
- An EF Core migration is required for the Jobs tables. Generate it explicitly as a separate
  user action; do not generate one as part of this implementation.

### Part 3 - Introduce durable source/result storage boundaries

Remove dependence on process-memory inputs.

Design durable storage for source files and generated artifacts.

Prefer object-storage-oriented abstractions for large files.

Update speech-specific job data to contain durable references/snapshots required for execution.

The Worker must be able to execute after API restart without requiring anything that existed only in API memory.

Implemented in this part:

- `SpeechGenerationRequest` stores owner, canonical provider, file name, version, options snapshot,
  and immutable source/text object IDs. The request, Job, initial event and Outbox record commit
  in one PostgreSQL transaction before dispatch.
- Equivalent submissions use an owner-scoped fingerprint covering provider, options, file name,
  extracted text and source hash. They return the original Job/Input IDs, including terminal jobs;
  a failed or cancelled operation is not silently submitted again as a new paid generation.
- `IArtifactStorage` owns binary content. Its initial filesystem adapter writes immutable objects
  under `AppDataPath/FileStorage/objects`; Docker mounts a persistent `artifacts-data` volume.
  A future Worker using this adapter must mount the same storage. Multi-host deployment requires
  shared durable storage or an object-storage adapter, not separate local disks.
- `AudioFile` contains only metadata and `ContentId`; it has no `Data` property.
  Audio bytes are passed separately to the repository and written through `IArtifactStorage`.
  Sample retrieval and authenticated downloads read content through the storage abstraction.
  Replacing that adapter does not change `JobDispatchMessage`.
- The existing local queue now carries IDs only. To keep the current application coherent, its
  adapter claims/renews the Job lease and records completion/failure/cancellation. Execution
  reloads input by ID and commits result metadata with Job completion in the same transaction.
  Cancellation intent is persisted before signalling the local executor.
- Artifact writes finish before database commit but cannot join its transaction. Failed or
  uncertain commits and replaced/deleted metadata can leave orphan objects. Automatic cleanup
  remains deferred until retention/reconciliation rules are defined; objects are not deleted
  on an uncertain commit.

Delivery after an API restart, recovery polling and durable progress/event delivery still require
Parts 4/5. The current local queue remains the sole temporary execution adapter until the Worker
is introduced. A stopped process can leave a Pending/Running Job, while
all input needed by the future Worker is already durable.

An EF Core migration is required for `SpeechGenerationRequest`, `AudioFile.ContentId` and removal
of the persisted `AudioFile.Data` column. The user generates it. Tests continue to use migrations,
without schema-creation substitutes. Existing audio bytes must be exported to the new storage
before dropping that column if existing data is to be retained.

### Part 4 - Transactional Outbox and RabbitMQ

Introduce:

- durable OutboxMessage
- Outbox Publisher
- RabbitMQ configuration
- job dispatch messages

Assume at-least-once delivery from the beginning.

Do not add an in-memory queue fallback or a second execution path.

Implemented in this part:

- `OutboxPublisherService` runs in the API host and reads the existing Outbox table. Each attempt
  takes one PostgreSQL row lock with `FOR UPDATE SKIP LOCKED`, allowing other publisher processes
  to work on different records. The lock lasts through publication and the database commit.
- `RabbitMqJobDispatchPublisher` reuses a connection/channel, enables publisher confirms and
  sends persistent messages with `mandatory: true`. An unroutable message is a failed publication.
  The publish timeout is 10 seconds by default; reconnecting is driven by Outbox retries.
- `PublishedAt` is saved only after confirmation. Failure rolls back the database transaction;
  the host retries after 5 seconds by default. If confirmation succeeded but its database commit
  did not, the message may be sent again with the same Outbox ID in AMQP `MessageId`.
- The JSON body remains `JobDispatchMessage`: JobId, ContractVersion and CorrelationId only.
  The consumer must tolerate duplicates through Jobs ownership; `MessageId` alone does not
  implement deduplication in RabbitMQ.
- The default topology is a durable direct exchange `tts.jobs`, durable queue `tts.jobs.dispatch`
  and routing key `dispatch`. No TTL/automatic queue deletion is configured. Compose gives the
  broker a stable hostname and persistent volume. This is a single-node topology, not HA.
- RabbitMQ is configured through the `RabbitMq` configuration section. Local API defaults use
  localhost:5672 and development credentials; Compose supplies individual connection fields.
  Deployment must set matching `RABBITMQ_USER` and `RABBITMQ_PASSWORD`.
  Management/AMQP host ports bind to loopback.
- Integration tests use isolated PostgreSQL, RabbitMQ and filesystem fixtures. No new migration
  is needed for Part 4; it uses the existing Outbox columns and index.

This stage publishes messages but does not add a consumer. Existing local speech execution is
unchanged and is not a RabbitMQ fallback. At Part 5 cutover, remove that execution registration
and direct enqueue calls before enabling the Worker consumer. Queued messages for already
terminal jobs must be acknowledged without another provider call. Automatic execution after
an API restart, consumer ACK/NACK policy and abandoned-attempt recovery remain Part 5 work.

Publisher confirmation behavior follows the official
[RabbitMQ .NET publisher confirms guide](https://www.rabbitmq.com/tutorials/tutorial-seven-dotnet).

### Part 5 - Separate Worker service

Create the independent Worker project/service.

It should:

- consume job messages;
- load durable job state;
- acquire execution ownership;
- resolve a handler based on job type;
- execute the speech handler;
- create attempts/history;
- persist progress;
- handle cancellation;
- complete/fail jobs;
- safely tolerate duplicate messages;
- support abandoned-job recovery.

The Worker must not reference the API project.

Implemented in this part:

- `TextToSpeech.Worker` is a separate host and Compose service, referencing Infra and Core only.
  Full generation runs exclusively there. The API keeps request acceptance, Outbox publication,
  sample generation and client communication; the temporary local queue and dispatcher are removed.
- RabbitMQ consumers use manual acknowledgements and one unacknowledged delivery per channel.
  `Worker:Concurrency` defaults to 2 per process. A delivery is acknowledged after its durable outcome;
  a connection/processing infrastructure failure leaves it unacknowledged for redelivery.
- The worker loads the authoritative job, resolves a keyed handler, validates contract/input versions
  and atomically claims an attempt. Live duplicates and already terminal jobs never invoke the provider again.
  Malformed/unsupported messages are published with confirms to `tts.jobs.dispatch.rejected` before ACK.
  They require inspection and replay after correcting the contract/handler; they are not retried in a loop.
- Leases use database time. The default lease is 60 seconds, renewed every second while persisting
  monotonic progress and checking durable cancellation. Each heartbeat has a separate database scope.
  Speech result metadata and completion still commit in the same existing transaction.
- Recovery scans expired attempts every 5 seconds and marks them `RecoveryRequired`.
  An uncertain external provider call is never automatically repeated. Existing `ResolveRecoveryAsync`
  requires an explicit safety decision before creating a retry/outbox record.
- The API relays changed persisted speech statuses to its connected clients once per second.
  New/reconnected SignalR connections receive current owned job states. Worker memory and SignalR
  delivery are not authoritative. Repeat submissions request a fresh snapshot from the same relay;
  they do not publish a competing captured status. The existing 200 ms delay and comment remain,
  including the immediate notification when an already completed audio result is reused.
- API and Worker share `artifacts-data` in Compose. The message body remains IDs/version/correlation only;
  source and MP3 bytes are resolved through storage. The API applies migrations before Worker startup.
- Integration tests start separate API/Worker service containers with isolated SQL, RabbitMQ and files.
  They cover generation/download for all existing providers and duplicate execution with durable cancellation.
  No new EF migration is required for this part.

Operational follow-up still required before treating the entire design as production-complete:

- provider-specific reconciliation and an operator path for `RecoveryRequired` and rejected messages;
- retention/cleanup of jobs, events, Outbox records and unreferenced artifacts;
- provider-wide rate limits across replicas, broker high availability and operational metrics/alerts;
- bounded history reads for long-lived owners in the status relay as job history grows.

Local verification uses `scripts/start-test-env.ps1`. Worker settings can be overridden with
`Worker__Concurrency`, `Worker__LeaseSeconds`, `Worker__HeartbeatSeconds` and
`Worker__RecoveryIntervalSeconds`. RabbitMQ uses the shared `RabbitMq:RabbitMqConnection` fields;
Compose supplies host, port, user, password and virtual host to both processes.

## 24. Cross-cutting requirements

The detailed implementation design must explicitly cover:

- at-least-once delivery;
- duplicate message handling;
- job idempotency;
- provider-side duplicate-cost risk;
- Worker crash recovery;
- execution leases;
- message redelivery vs job retry;
- durable cancellation;
- immutable/durable job input;
- object storage for large artifacts;
- transaction boundaries;
- outbox failure/recovery;
- concurrency and provider rate limits;
- authorization/ownership;
- API state recovery after reconnect;
- Web/Android/iOS compatibility;
- job and message versioning;
- correlation IDs and structured observability;
- retention/cleanup for jobs, attempts, events and outbox records;
- safe deployment while old jobs from a previous application version may still exist.

## 25. What not to optimize for

Do not optimize for:

- the smallest diff;
- preserving the current background queue;
- minimizing the number of new projects/classes;
- avoiding migrations;
- keeping current responsibilities together;
- maintaining compatibility with a deleted production environment.

Optimize for:

- clear ownership
- durability
- extensibility
- failure recovery
- observability
- testability
- independent scaling
- future clients
- future job types

## 26. Expected next action from Codex

Do not implement the full architecture immediately.

First:

1. Inspect the current dev branch.
2. Trace the complete existing full-speech-generation lifecycle.
3. Compare the current implementation against this architecture.
4. Identify conflicts, hidden coupling, and decisions that still require confirmation.
5. Evaluate the custom BackgroundJob + RabbitMQ approach against an appropriate existing job/workflow framework before implementing the generic jobs subsystem.
6. Produce a detailed implementation plan for Part 1 only.
7. Include affected code areas, target responsibilities, removal/movement of existing responsibilities, tests, risks, and completion criteria.
8. Do not preserve an existing structure merely because changing it would require a larger refactor.

The architecture should be designed so that adding another job type, another Worker instance, Android/iOS clients, or eventually extracting the jobs subsystem does not require another fundamental redesign.
