import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../services/api.service';
import { CommonModule } from '@angular/common';

@Component({
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <h2>Citizen Street Light Installation Request</h2>
    <form [formGroup]="form" (ngSubmit)="submit()" class="card">
      <input formControlName="citizenName" placeholder="Citizen Name" />
      <input formControlName="citizenMobile" placeholder="Mobile" />
      <input formControlName="ward" placeholder="Ward" />
      <textarea formControlName="address" placeholder="Address"></textarea>
      <textarea formControlName="description" placeholder="Installation location details"></textarea>
      <button [disabled]="submitted || form.invalid" type="submit">Submit</button>
    </form>
    <p *ngIf="applicationNumber">Application Number: <strong>{{ applicationNumber }}</strong></p>
  `
})
export class CitizenFormComponent {
  private fb = inject(FormBuilder);
  private api = inject(ApiService);

  submitted = false;
  applicationNumber = '';

  form = this.fb.group({
    citizenName: ['', Validators.required],
    citizenMobile: ['', Validators.required],
    ward: ['', Validators.required],
    address: ['', Validators.required],
    description: ['', Validators.required]
  });

  submit() {
    if (this.form.invalid || this.submitted) return;

    this.api.submitCitizenRequest(this.form.value).subscribe((res: any) => {
      this.applicationNumber = res.applicationNumber;
      this.submitted = true;
      this.form.disable();
    });
  }
}
