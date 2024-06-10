import { Component, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { FormGroup, FormControl, ReactiveFormsModule } from '@angular/forms';


@Component({
  selector: 'usersettings',
  templateUrl: './usersettings.component.html'
})
export class UserSettingsComponent {
  public settings: UserSettings;
  public profileForm: FormGroup;
  private http: HttpClient;
  private baseUrl: string;

  constructor(http: HttpClient, @Inject('BASE_URL') baseUrl: string) {
    this.http = http;
    this.baseUrl = baseUrl;
    this.GetSettings();
  }

    private GetSettings() {
        this.http.get<UserSettings>(this.baseUrl + 'usersettings/getitem').subscribe(result => {
            this.settings = result;
          this.profileForm = new FormGroup({
            phone: new FormControl(this.settings.phone),
            email: new FormControl(this.settings.email),
            });
        }, error => console.error(error));
    }

  public OnSubmit() {
    const body = new UserSettingsUpdater();
    body.phone = this.profileForm.value.phone;
    body.email = this.profileForm.value.email;

    this.http.post<UserSettings>(this.baseUrl + 'usersettings/update', body).subscribe(result => {
      console.info("Настройки сохранены");
    }, error => console.error(error));
  }
}

interface UserSettings {
  phone: string;
  email: string;

}

class UserSettingsUpdater {
  phone: string;
  email: string;
}
