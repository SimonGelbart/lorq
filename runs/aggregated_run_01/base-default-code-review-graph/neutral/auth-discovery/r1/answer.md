Source-verified candidate paths:

### Core authentication flows

- [CustomerController.cs](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Controllers/CustomerController.cs:442) — primary HTTP endpoints:
  - Login and password validation at lines 442–482.
  - MFA login redirect/session setup around line 495.
  - MFA verification entry at line 720.
  - Password recovery/reset at lines 809–933.
  - Registration at lines 959–1044.
  - MFA settings/configuration at line 2188.

- [CustomerRegistrationService.cs](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Customers/CustomerRegistrationService.cs:138) — core business logic:
  - Credential validation, lockout, and MFA requirement detection.
  - Customer registration at line 238.
  - Password hashing/change at line 364.
  - Final cookie sign-in and login events at line 450.

- [ICustomerRegistrationService.cs](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Customers/ICustomerRegistrationService.cs) — authentication/registration service contract.

- [CustomerService.cs](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Customers/CustomerService.cs:1519) — validates password-recovery tokens and expiration dates.

- [CookieAuthenticationService.cs](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Authentication/CookieAuthenticationService.cs:45) — creates claims, issues/removes authentication cookies, and resolves the authenticated customer.

- [ExternalAuthenticationService.cs](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Authentication/External/ExternalAuthenticationService.cs:270) — external-provider authentication, account association, auto-registration, and eventual `SignInCustomerAsync` calls.

### Routing and UI

- [RouteProvider.cs](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Infrastructure/RouteProvider.cs:35) — maps `/login`, `/register`, `/multi-factor-verification`, and password-recovery routes to `CustomerController`.

- [CustomerModelFactory.cs](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Factories/CustomerModelFactory.cs:406) — prepares registration, login, recovery, and MFA models.

- [Models/Customer](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Models/Customer) — notably `LoginModel`, `RegisterModel`, `PasswordRecoveryModel`, `PasswordRecoveryConfirmModel`, and MFA models.

- [Validators/Customer](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Validators/Customer) — validates credentials, registration fields, recovery email, and new-password confirmation. For example, [PasswordRecoveryConfirmValidator.cs](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Validators/Customer/PasswordRecoveryConfirmValidator.cs:13) applies password policy and matching checks.

- [Views/Customer](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Views/Customer) — `Login.cshtml`, `Register.cshtml`, password-recovery views, and MFA configuration/verification views.

### Multi-factor authentication

- [MultiFactorAuthenticationPluginManager.cs](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Authentication/MultiFactor/MultiFactorAuthenticationPluginManager.cs:40) — discovers active MFA plugins and checks whether the customer’s selected provider is active.

- [IMultiFactorAuthenticationMethod.cs](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Authentication/MultiFactor/IMultiFactorAuthenticationMethod.cs) — provider plugin contract.

- [ForceMultiFactorAuthenticationAttribute.cs](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__auth-discovery__r1/src/Presentation/Nop.Web.Framework/Mvc/Filters/ForceMultiFactorAuthenticationAttribute.cs:62) — redirects customers to MFA setup when MFA is mandatory but no provider is selected.

- [Google Authenticator AuthenticationController.cs](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__auth-discovery__r1/src/Plugins/Nop.Plugin.MultiFactorAuth.GoogleAuthenticator/Controllers/AuthenticationController.cs:80) — handles second-factor login verification and completes sign-in at line 97.

- [GoogleAuthenticatorService.cs](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__auth-discovery__r1/src/Plugins/Nop.Plugin.MultiFactorAuth.GoogleAuthenticator/Services/GoogleAuthenticatorService.cs:193) — validates TOTP tokens.

### Authentication setup and tests

- [AuthenticationStartup.cs](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__auth-discovery__r1/src/Presentation/Nop.Web.Framework/Infrastructure/AuthenticationStartup.cs:18) and [ServiceCollectionExtensions.cs](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__auth-discovery__r1/src/Presentation/Nop.Web.Framework/Infrastructure/Extensions/ServiceCollectionExtensions.cs:255) — register authentication middleware and main/external cookie schemes.

- [CustomerRegistrationServiceTests.cs](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__auth-discovery__r1/src/Tests/Nop.Tests/Nop.Services.Tests/Customers/CustomerRegistrationServiceTests.cs:69) — covers registered-customer login, clear/encrypted/hashed passwords, and password changes.

- [Validator tests](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__auth-discovery__r1/src/Tests/Nop.Tests/Nop.Web.Tests/Public/Validators/Customer) — coverage for login, registration, and both password-recovery validators.
