import { Component, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { FormGroup, FormControl, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterModule, Routes } from '@angular/router';


@Component({
  selector: 'auth',
  templateUrl: './auth.component.html'
})

export class AuthComponent {
  public auth: Auth;
  public authForm: FormGroup;
  public registerForm: FormGroup;
  private http: HttpClient;
  private baseUrl: string;
  public error_message: string;

  constructor(http: HttpClient, @Inject('BASE_URL') baseUrl: string, private router: Router) {
    this.http = http;
    this.baseUrl = baseUrl;
    this.authForm = new FormGroup({
      login: new FormControl(''),
      password: new FormControl(''),
    });
    this.registerForm = new FormGroup({
      login: new FormControl(''),
      password: new FormControl(''),
    });
  }

  public OnAuth() {
    const body = new Auth();
    body.login = this.authForm.value.login;
    body.password = this.authForm.value.password;

    this.http.post<AuthResult>(this.baseUrl + 'user/auth', body).subscribe(result => {
      console.info("Авторизация успешна");
      alert("Авторизация успешна");
      this.router.navigate(['/game']);
    }, error => {
      console.error(error);
      this.error_message = error;
    });
  }

  public OnRegister() {
    const body = new Register();
    body.login = this.registerForm.value.login;
    body.password = this.registerForm.value.password;

    this.http.post<AuthResult>(this.baseUrl + 'user/register', body).subscribe(result => {
      console.info("Регистрация успешна");
      alert("Регистрация успешна. Для продолжения заполните свои данные.");
      this.router.navigate(['/usersettings']);
    }, error => {
      console.error(error);
      this.error_message = error;
    });
  }
}

class Auth {
  login: string;
  password: string;

}

interface AuthResult {
  userId: string;
  token: string;

}

class Register {
  login: string;
  password: string;

}

