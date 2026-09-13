import { inject, Injectable } from "@angular/core";
import { MatSnackBar } from "@angular/material/snack-bar";
import { TranslateService } from "@ngx-translate/core";
import { take } from "rxjs";

@Injectable({ providedIn: 'root' })
export class UiNotifyService {
  private readonly snack = inject(MatSnackBar);
  private readonly translate = inject(TranslateService);

  error(messageKey: string, durationMs = 4000): void {
    this.translate.get(messageKey).pipe(take(1)).subscribe((message) => {
      this.snack.open(message, undefined, { duration: durationMs });
    });
  }
}
