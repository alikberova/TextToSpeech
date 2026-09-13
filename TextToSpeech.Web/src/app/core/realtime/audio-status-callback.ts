export type AudioStatusCallback = (
  id: string,
  status: string,
  progress: number | null,
  error?: string
) => void;
