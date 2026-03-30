import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';

interface User {
  id: string;
  userName: string;
  email: string;
  roles: string[];
}

const ALL_ROLES = [
  'Administrator',
  'Admin',
  'Single',
  'Married',
  'Counselor'
];

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

  assignRole(user: User, role: string) {
    this.http.post(`/api/admin/users/${user.id}/roles/${role}`, {}).subscribe({
      next: () => {
        user.roles.push(role);
      },
      error: () => {
        this.error = `Failed to assign role ${role}`;
      }
    });
  }

  removeRole(user: User, role: string) {
    this.http.delete(`/api/admin/users/${user.id}/roles/${role}`).subscribe({
      next: () => {
        user.roles = user.roles.filter(r => r !== role);
      },
      error: () => {
        this.error = `Failed to remove role ${role}`;
      }
    });
  }

  getAssignableRoles(user: User): string[] {
    return ALL_ROLES.filter(role => !user.roles.includes(role));
  }
}
