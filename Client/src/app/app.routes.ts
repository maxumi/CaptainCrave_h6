import { Routes } from '@angular/router';
import { StartSite } from './feature/start-site/start-site';

export const routes: Routes = [
     { path: '', redirectTo: 'start', pathMatch: 'full' },
    { path: "start", component: StartSite }
];
