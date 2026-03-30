import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly baseUrl = 'http://localhost:5072/api';

  constructor(private http: HttpClient) {}

  submitCitizenRequest(payload: any) {
    return this.http.post(`${this.baseUrl}/citizen/requests`, payload);
  }

  trackStatus(applicationNumber: string) {
    return this.http.get(`${this.baseUrl}/citizen/requests/${applicationNumber}/status`);
  }

  requestOtp(mobile: string) {
    return this.http.post(`${this.baseUrl}/auth/request-otp`, { mobile });
  }

  verifyOtp(mobile: string, otp: string) {
    return this.http.post(`${this.baseUrl}/auth/verify-otp`, { mobile, otp });
  }

  listRequests() {
    return this.http.get(`${this.baseUrl}/internal/requests`);
  }

  updateStatus(requestId: number, status: string, remarks: string) {
    return this.http.post(`${this.baseUrl}/internal/requests/${requestId}/status`, { status, remarks });
  }

  updateEstimate(requestId: number, estimatedAmount: number) {
    return this.http.patch(
      `${this.baseUrl}/internal/requests/${requestId}/estimate?estimatedAmount=${estimatedAmount}`,
      {}
    );
  }

  uploadFile(requestId: number, payload: FormData) {
    return this.http.post(`${this.baseUrl}/internal/requests/${requestId}/files`, payload);
  }
}
