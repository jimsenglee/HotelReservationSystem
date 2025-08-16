# Configuration Setup Guide

## Environment Variables Setup

To run this application, you need to configure the following environment variables or update the `appsettings.json` file with your actual credentials:

### PayPal Configuration
```json
"PayPal": {
  "Key": "your-paypal-client-id",
  "Secret": "your-paypal-client-secret", 
  "mode": "sandbox" // Use "live" for production
}
```

### Stripe Configuration
```json
"StripeSettings": {
  "SecretKey": "sk_test_your_stripe_secret_key",
  "PublicKey": "pk_test_your_stripe_public_key",
  "WebhookSecret": "whsec_your_stripe_webhook_secret"
}
```

### Email Configuration (Gmail SMTP)
```json
"Smtp": {
  "User": "your-email@gmail.com",
  "Pass": "your-gmail-app-password", // Generate from Google Account settings
  "Name": "Hotel Admin🏨",
  "Host": "smtp.gmail.com",
  "Port": 587
}
```

### Twilio Configuration (SMS Services)
```json
"Twilio": {
  "AccountSid": "your-twilio-account-sid",
  "AuthToken": "your-twilio-auth-token"
}
```

### reCAPTCHA Configuration
```json
"RecaptchaSettings": {
  "SiteKey": "your-recaptcha-site-key",
  "SecretKey": "your-recaptcha-secret-key"
}
```

## How to Get These Credentials

1. **PayPal**: Register at [PayPal Developer](https://developer.paypal.com/)
2. **Stripe**: Register at [Stripe Dashboard](https://dashboard.stripe.com/)
3. **Gmail App Password**: Enable 2FA and generate app password in [Google Account Settings](https://myaccount.google.com/security)
4. **Twilio**: Register at [Twilio Console](https://console.twilio.com/)
5. **reCAPTCHA**: Register at [Google reCAPTCHA](https://www.google.com/recaptcha/)

## Security Note

⚠️ **Never commit real credentials to version control!** Always use environment variables or secure configuration management in production.
