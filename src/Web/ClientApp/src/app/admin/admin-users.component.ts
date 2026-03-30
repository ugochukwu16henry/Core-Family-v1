import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';

interface User {
  id: string;
  userName: string;
  email: string;
  roles: string[];
}

@Component({
  selector: 'app-admin-users',
  templateUrl: './admin-users.component.html',
  styleUrls: ['./admin-users.component.scss']
})
export class AdminUsersComponent implements OnInit {
  users: User[] = [];
  loading = false;
  error = '';

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.fetchUsers();
  }

  fetchUsers() {
    this.loading = true;
    this.http.get<User[]>('/api/admin/users').subscribe({
      next: users => {
        this.users = users;
        this.loading = false;
      },
      error: err => {
        this.error = 'Failed to load users';
        this.loading = false;
      }
    });
  }

  // Add methods for role management here
}
