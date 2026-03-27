export interface CreateCheckoutRequest {
  provider: 'stripe' | 'paystack' | 'google_pay';
  currency?: string;
  successUrl?: string;
  cancelUrl?: string;
}

export interface CheckoutSession {
  transactionId: string;
  type: string;
  status: 'Pending' | 'Completed' | 'Failed' | 'Refunded';
  amount: number;
  currency: string;
  provider: string;
  checkoutUrl?: string | null;
  externalTransactionId?: string | null;
  requiresRedirect: boolean;
}

export interface TransactionSummary {
  id: string;
  type: string;
  amount: number;
  currency: string;
  paymentMethod: string;
  status: string;
  referenceId?: string | null;
  externalTransactionId?: string | null;
  createdAt: string;
  failureReason?: string | null;
}
