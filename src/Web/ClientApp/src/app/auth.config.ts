import { AuthConfig } from 'angular-oauth2-oidc';

export const authConfig: AuthConfig = {
  issuer: 'https://localhost:5001', // IdentityServer URL
  redirectUri: window.location.origin + '/auth-callback',
  clientId: 'angular_spa',
  responseType: 'code',
  scope: 'openid profile api1',
  showDebugInformation: true,
  requireHttps: false, // Set to true in production!
};
