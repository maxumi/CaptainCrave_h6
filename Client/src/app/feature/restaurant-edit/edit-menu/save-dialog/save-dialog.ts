import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogActions, MatDialogClose, MatDialogContent, MatDialogRef, MatDialogTitle } from '@angular/material/dialog';
import { TranslocoModule } from '@jsverse/transloco';

/**
 * Component for displaying a save confirmation dialog when editing a menu item.
 */
@Component({
  selector: 'app-save-dialog',
  imports: [TranslocoModule,MatButtonModule, MatDialogActions, MatDialogClose, MatDialogTitle, MatDialogContent],
  templateUrl: './save-dialog.html',
  styleUrl: './save-dialog.css',
})
export class SaveDialog {
  readonly dialogRef = inject(MatDialogRef<SaveDialog>);

}
