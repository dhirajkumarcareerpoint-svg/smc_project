import { Component } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink],
  template: `
    <header class="topbar">
      <h1>SMC Street Light Request System</h1>
      <nav>
        <a routerLink="/">Citizen Form</a>
        <a routerLink="/track">Track Status</a>
        <a routerLink="/login">Internal Login</a>
        <a routerLink="/dashboard">Dashboard</a>
      </nav>
    </header>
    <main class="container"><router-outlet></router-outlet></main>
  `
})
export class AppComponent {}
