import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogActions, MatDialogClose, MatDialogContent, MatDialogRef, MatDialogTitle } from '@angular/material/dialog';
import { TranslocoModule } from '@jsverse/transloco';

@Component({
  selector: 'app-delete-dialog',
  imports: [TranslocoModule,MatButtonModule, MatDialogActions, MatDialogClose, MatDialogTitle, MatDialogContent],
  templateUrl: './delete-dialog.html',
  styleUrl: './delete-dialog.css',
})
export class DeleteDialog {
  readonly dialogRef = inject(MatDialogRef<DeleteDialog>);
}
