import { Component, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { ProgramsService } from '../../core/services/programs.service';
import { Certificate } from '../../core/models/program.model';

@Component({
  selector: 'app-certificates',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './certificates.component.html',
  styleUrl: './certificates.component.scss'
})
export class CertificatesComponent {
  private readonly programsService = inject(ProgramsService);
  private readonly route = inject(ActivatedRoute);

  certificates = signal<Certificate[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);
  downloadingId = signal<string | null>(null);

  constructor() {
    effect(() => {
      this.loadCertificates();
    });
  }

  private loadCertificates(): void {
    this.loading.set(true);
    this.error.set(null);

    this.programsService.getMyCertificates().subscribe({
      next: (data) => {
        this.certificates.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err?.error?.message || 'Failed to load certificates');
        this.loading.set(false);
      }
    });
  }

  downloadCertificate(certificate: Certificate): void {
    this.downloadingId.set(certificate.id);

    this.programsService.downloadCertificate(certificate.programId || '').subscribe({
      next: (blob) => {
        const link = document.createElement('a');
        link.href = window.URL.createObjectURL(blob);
        link.download = `${certificate.certificateCode}.pdf`;
        link.click();
        window.URL.revokeObjectURL(link.href);
        this.downloadingId.set(null);
      },
      error: (err) => {
        // Fallback: open PDF URL in new tab if available
        if (certificate.pdfUrl) {
          window.open(certificate.pdfUrl, '_blank');
        }
        this.downloadingId.set(null);
      }
    });
  }

  viewCertificate(certificate: Certificate): void {
    // Open certificate in new tab
    window.open(certificate.pdfUrl, '_blank');
  }

  copyCertificateCode(certificateCode: string): void {
    navigator.clipboard.writeText(certificateCode).then(() => {
      // Could show a toast notification here
      console.log('Certificate code copied to clipboard');
    });
  }

  shareCertificate(certificate: Certificate): void {
    const shareUrl = `${window.location.origin}/certificates/${certificate.id}`;
    const shareText = `Check out my certificate! I completed ${certificate.programTitle}`;

    if (navigator.share) {
      navigator.share({
        title: 'My Certificate',
        text: shareText,
        url: shareUrl
      });
    } else {
      // Fallback: copy share link to clipboard
      navigator.clipboard.writeText(shareUrl);
      alert('Share link copied to clipboard!');
    }
  }

  getDownloadButtonText(certificateId: string): string {
    return this.downloadingId() === certificateId ? 'Downloading...' : 'Download';
  }
}
