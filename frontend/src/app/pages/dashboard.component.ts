import { Component } from '@angular/core';
import { ApiService } from '../services/api.service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <h2>Internal Dashboard</h2>
    <p>Logged in as: <strong>{{ role }}</strong></p>

    <div class="card" *ngFor="let req of requests">
      <p><strong>{{ req.applicationNumber }}</strong> - {{ req.currentStatus }}</p>
      <p>Citizen: {{ req.citizenName }} | Ward: {{ req.ward }}</p>
      <p>Estimated Amount: {{ req.estimatedAmount ?? 'Not set' }}</p>

      <div *ngIf="role === 'JE'">
        <input type="number" [(ngModel)]="req.estimateAmountInput" placeholder="Estimate Amount (INR)" />
        <button (click)="setEstimate(req)">Save Estimate</button>

        <label>Site Photo</label>
        <input type="file" (change)="onSelectFile(req, $event, 'SitePhoto')" />
        <label>Geo-tagged Photo</label>
        <input type="file" (change)="onSelectFile(req, $event, 'GeoTaggedPhoto')" />
        <input [(ngModel)]="req.latitude" placeholder="Latitude" />
        <input [(ngModel)]="req.longitude" placeholder="Longitude" />
        <button (click)="uploadJeFiles(req)">Upload JE Photos</button>

        <label>JE Estimate File</label>
        <input type="file" (change)="onSelectFile(req, $event, 'JeEstimate')" />
        <button (click)="uploadJeEstimateFile(req)">Upload JE Estimate File</button>
      </div>

      <div *ngIf="role === 'AE' && isAeUploadAllowed(req)">
        <label>AE Supporting File (1 to 10 lakh)</label>
        <input type="file" (change)="onSelectFile(req, $event, 'AeSupporting')" />
        <button (click)="uploadAeSupporting(req)">Upload AE Supporting File</button>
      </div>

      <div *ngIf="canAct()">
        <select [(ngModel)]="req.nextStatus">
          <option value="">Select next status</option>
          <option *ngFor="let s of statusOptions()" [value]="s">{{ s }}</option>
        </select>
        <input [(ngModel)]="req.remarks" placeholder="Remarks" />
        <button (click)="update(req)">Update Status</button>
      </div>
    </div>
  `
})
export class DashboardComponent {
  role = localStorage.getItem('role') || 'Unknown';
  requests: any[] = [];

  constructor(private api: ApiService) {
    this.api.listRequests().subscribe((res: any) => {
      this.requests = (res || []).map((x: any) => ({
        ...x,
        nextStatus: '',
        remarks: '',
        estimateAmountInput: x.estimatedAmount ?? '',
        latitude: '',
        longitude: '',
        jeSitePhoto: null,
        jeGeoPhoto: null,
        jeEstimateFile: null,
        aeSupportingFile: null
      }));
    });
  }

  canAct() {
    return ['JE', 'AE', 'DeputyEngineer', 'ExecutiveEngineer', 'Admin'].includes(this.role);
  }

  statusOptions() {
    const map: Record<string, string[]> = {
      JE: [
        'Pending JE Verification',
        'Site Verified (Feasible)',
        'Site Verified (Not Feasible)',
        'Estimate Uploaded by JE',
        'Re-Visit Required'
      ],
      AE: ['Approved by AE', 'Re-Visit Required'],
      DeputyEngineer: ['Recommended by Deputy Engineer', 'Rejected'],
      ExecutiveEngineer: ['Approved (Sanctioned)', 'Rejected', 'HOLD', 'Send Back', 'Completed'],
      Admin: [
        'Submitted',
        'Pending JE Verification',
        'Site Verified (Feasible)',
        'Site Verified (Not Feasible)',
        'Estimate Uploaded by JE',
        'Approved by AE',
        'Re-Visit Required',
        'Recommended by Deputy Engineer',
        'Approved (Sanctioned)',
        'Rejected',
        'HOLD',
        'Send Back',
        'Completed'
      ]
    };
    return map[this.role] || [];
  }

  onSelectFile(req: any, event: Event, fileType: string) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    if (fileType === 'SitePhoto') req.jeSitePhoto = file;
    if (fileType === 'GeoTaggedPhoto') req.jeGeoPhoto = file;
    if (fileType === 'JeEstimate') req.jeEstimateFile = file;
    if (fileType === 'AeSupporting') req.aeSupportingFile = file;
  }

  setEstimate(req: any) {
    if (this.role !== 'JE' || !req.estimateAmountInput) return;
    this.api.updateEstimate(req.id, Number(req.estimateAmountInput)).subscribe(() => {
      req.estimatedAmount = Number(req.estimateAmountInput);
    });
  }

  uploadJeFiles(req: any) {
    if (this.role !== 'JE') return;
    if (req.jeSitePhoto) {
      const site = new FormData();
      site.append('file', req.jeSitePhoto);
      site.append('fileType', 'SitePhoto');
      this.api.uploadFile(req.id, site).subscribe();
    }
    if (req.jeGeoPhoto) {
      const geo = new FormData();
      geo.append('file', req.jeGeoPhoto);
      geo.append('fileType', 'GeoTaggedPhoto');
      geo.append('latitude', req.latitude || '');
      geo.append('longitude', req.longitude || '');
      geo.append('capturedAtUtc', new Date().toISOString());
      this.api.uploadFile(req.id, geo).subscribe();
    }
  }

  uploadJeEstimateFile(req: any) {
    if (this.role !== 'JE' || !req.jeEstimateFile) return;
    const form = new FormData();
    form.append('file', req.jeEstimateFile);
    form.append('fileType', 'JeEstimate');
    this.api.uploadFile(req.id, form).subscribe();
  }

  isAeUploadAllowed(req: any) {
    const amount = Number(req.estimatedAmount || 0);
    return amount >= 100000 && amount <= 1000000;
  }

  uploadAeSupporting(req: any) {
    if (this.role !== 'AE' || !req.aeSupportingFile || !this.isAeUploadAllowed(req)) return;
    const form = new FormData();
    form.append('file', req.aeSupportingFile);
    form.append('fileType', 'AeSupporting');
    this.api.uploadFile(req.id, form).subscribe();
  }

  update(req: any) {
    if (!req.nextStatus) return;
    this.api.updateStatus(req.id, req.nextStatus, req.remarks).subscribe((res: any) => {
      req.currentStatus = res.currentStatus;
      req.nextStatus = '';
      req.remarks = '';
    });
  }
}
