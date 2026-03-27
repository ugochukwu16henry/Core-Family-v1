import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { TransactionSummary } from '../../core/models/payment.model';
import { PaymentsService } from '../../core/services/payments.service';

@Component({
  selector: 'app-billing',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './billing.component.html',
  styleUrl: './billing.component.scss'
})
export class BillingComponent {
  private readonly paymentsService = inject(PaymentsService);

  readonly transactions = signal<TransactionSummary[]>([]);
  readonly loading = signal(true);
  readonly actionBusy = signal(false);
  readonly error = signal<string | null>(null);

  refundReasonById: Record<string, string> = {};

  constructor() {
    this.loadTransactions();
  }

  refresh(): void {
    this.loadTransactions();
  }

  requestRefund(transactionId: string): void {
    if (this.actionBusy()) {
      return;
    }

    this.actionBusy.set(true);
    this.error.set(null);

    const reason = this.refundReasonById[transactionId]?.trim() || '';
    this.paymentsService.requestRefund(transactionId, { reason }).subscribe({
      next: (updated) => {
        this.transactions.set(this.transactions().map((item) => item.id === updated.id ? updated : item));
        this.refundReasonById[transactionId] = '';
        this.actionBusy.set(false);
      },
      error: (err) => {
        this.error.set(err?.error?.message || 'Could not submit refund request.');
        this.actionBusy.set(false);
      }
    });
  }

  isRefundRequested(transaction: TransactionSummary): boolean {
    return transaction.failureReason?.startsWith('REFUND_REQUESTED:') ?? false;
  }

  getRefundReason(transaction: TransactionSummary): string | null {
    if (!transaction.failureReason) {
      return null;
    }

    if (this.isRefundRequested(transaction)) {
      return transaction.failureReason.replace('REFUND_REQUESTED:', '').trim();
    }

    return transaction.failureReason;
  }

  canRequestRefund(transaction: TransactionSummary): boolean {
    return transaction.status === 'Completed' && !this.isRefundRequested(transaction);
  }

  private loadTransactions(): void {
    this.loading.set(true);
    this.error.set(null);

    this.paymentsService.getMyTransactions().subscribe({
      next: (transactions) => {
        this.transactions.set(transactions);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err?.error?.message || 'Could not load transactions.');
        this.loading.set(false);
      }
    });
  }
}
