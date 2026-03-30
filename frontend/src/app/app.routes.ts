import { Routes } from '@angular/router';
import { CitizenFormComponent } from './pages/citizen-form.component';
import { TrackStatusComponent } from './pages/track-status.component';
import { LoginComponent } from './pages/login.component';
import { DashboardComponent } from './pages/dashboard.component';

export const routes: Routes = [
  { path: '', component: CitizenFormComponent },
  { path: 'track', component: TrackStatusComponent },
  { path: 'login', component: LoginComponent },
  { path: 'dashboard', component: DashboardComponent },
  { path: '**', redirectTo: '' }
];
