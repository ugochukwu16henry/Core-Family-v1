import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../auth.service';

@Component({
  standalone: false,
  selector: 'app-nav-menu',
  templateUrl: './nav-menu.component.html',
  styleUrls: ['./nav-menu.component.scss']
})
export class NavMenuComponent {

  get isAuthenticated(): boolean {
    return this.authService.isAuthenticated;
  }

  constructor(private authService: AuthService, private router: Router) {}

  login(event: Event): void {
    event.preventDefault();
    this.authService.login();
  }

  logout(event: Event): void {
    event.preventDefault();
    this.authService.logout();
  }
}
