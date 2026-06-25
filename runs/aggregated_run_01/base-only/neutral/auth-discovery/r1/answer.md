The main authentication flow is concentrated in these files.

### Routing and request orchestration

- [RouteProvider.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Infrastructure/RouteProvider.cs:35) — maps `/login`, `/register`, `/logout`, MFA verification, and password-recovery routes to `CustomerController`.
- [CustomerController.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Controllers/CustomerController.cs:442) — primary public endpoint controller:
  - login and credential validation at lines 442–504;
  - MFA challenge/session setup at lines 493–504 and verification at 720;
  - logout at 752–781;
  - password recovery/reset at 809–950;
  - registration at 959–1044;
  - customer MFA configuration at 2188–2263.

### Core authentication and customer services

- [CustomerRegistrationService.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Customers/CustomerRegistrationService.cs:138) — validates credentials, lockout state and MFA requirements; registers customers and hashes/encrypts passwords at line 238; completes sign-in at line 452.
- [CustomerRegistrationRequest.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Customers/CustomerRegistrationRequest.cs) — input contract for registration.
- [CustomerService.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Customers/CustomerService.cs:1519) — validates recovery tokens and their expiration.
- [CookieAuthenticationService.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Authentication/CookieAuthenticationService.cs:45) — creates the claims principal and issues/removes the authentication cookie.
- [ServiceCollectionExtensions.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__auth-discovery__r1/src/Presentation/Nop.Web.Framework/Infrastructure/Extensions/ServiceCollectionExtensions.cs:255) — configures the main and external cookie schemes, login path and cookie security.
- [AuthenticationStartup.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__auth-discovery__r1/src/Presentation/Nop.Web.Framework/Infrastructure/AuthenticationStartup.cs:15) and [AuthenticationMiddleware.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Authentication/AuthenticationMiddleware.cs:36) — register the authentication pipeline and populate `HttpContext.User`.
- [ExternalAuthenticationService.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Authentication/External/ExternalAuthenticationService.cs:270) — external-provider authentication, account association, auto-registration and sign-in.

### Password recovery

- [MessageTokenProvider.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Messages/MessageTokenProvider.cs:1330) — constructs the recovery-confirmation URL containing the token and customer GUID.
- [WorkflowMessageService.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Messages/WorkflowMessageService.cs:319) — sends the password-recovery email.
- [NopCustomerDefaults.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__auth-discovery__r1/src/Libraries/Nop.Core/Domain/Customers/NopCustomerDefaults.cs:66) — defines recovery-token, generation-date and selected-MFA-provider attribute keys.

### Multi-factor authentication

- [IMultiFactorAuthenticationMethod.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Authentication/MultiFactor/IMultiFactorAuthenticationMethod.cs:8) — plugin contract for MFA configuration and verification components.
- [MultiFactorAuthenticationPluginManager.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Authentication/MultiFactor/MultiFactorAuthenticationPluginManager.cs:54) — loads active MFA providers and checks whether the selected provider is active.
- [CustomerModelFactory.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Factories/CustomerModelFactory.cs:979) — builds MFA provider models and selects either configuration or login-verification components.
- [GoogleAuthenticatorMethod.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__auth-discovery__r1/src/Plugins/Nop.Plugin.MultiFactorAuth.GoogleAuthenticator/GoogleAuthenticatorMethod.cs:53) — concrete MFA plugin exposing configuration and verification components.
- [AuthenticationController.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__auth-discovery__r1/src/Plugins/Nop.Plugin.MultiFactorAuth.GoogleAuthenticator/Controllers/AuthenticationController.cs:54) — registers authenticator secrets, validates codes and completes customer sign-in after successful MFA.

### Public UI and validation

- Views: [Login.cshtml](/home/simon/repos/lorq-worktrees/base-only__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Views/Customer/Login.cshtml:1), [Register.cshtml](/home/simon/repos/lorq-worktrees/base-only__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Views/Customer/Register.cshtml:1), [PasswordRecovery.cshtml](/home/simon/repos/lorq-worktrees/base-only__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Views/Customer/PasswordRecovery.cshtml:1), [PasswordRecoveryConfirm.cshtml](/home/simon/repos/lorq-worktrees/base-only__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Views/Customer/PasswordRecoveryConfirm.cshtml:1), [MultiFactorVerification.cshtml](/home/simon/repos/lorq-worktrees/base-only__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Views/Customer/MultiFactorVerification.cshtml:1).
- Input validation: [LoginValidator.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Validators/Customer/LoginValidator.cs:9), [RegisterValidator.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Validators/Customer/RegisterValidator.cs:12), [PasswordRecoveryValidator.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Validators/Customer/PasswordRecoveryValidator.cs:8), and [PasswordRecoveryConfirmValidator.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Validators/Customer/PasswordRecoveryConfirmValidator.cs:9).
