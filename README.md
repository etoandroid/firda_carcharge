# imicCharge

This project is a system for managing and processing payments for electric vehicle (EV) charging. It consists of a backend API and a cross-platform mobile/desktop application.

## Project Structure

The solution is divided into two main projects:

1.  **`imicCharge.API`**: An ASP.NET Core Web API built on .NET 9. This project handles all business logic, including user authentication, payment processing via Stripe, and will handle communication with the charging hardware.
2.  **`imicCharge.APP`**: A .NET MAUI application, also on .NET 9, which serves as the user-facing client for iOS, Android, and Windows.

## Technologies Used

* **Backend**: ASP.NET Core (.NET 9)
* **Frontend**: .NET MAUI (.NET 9)
* **Database**: Entity Framework Core with SQL Server
* **Authentication**: ASP.NET Core Identity
* **Payment Processing**: Stripe

## Getting Started

### Prerequisites

* .NET 9 SDK
* Visual Studio 2022 (with the .NET MAUI workload installed)
* An SQL Server instance
* Stripe account for API keys

### 1. Configure Backend Secrets

The API project uses the .NET Secret Manager for handling sensitive configuration data during development.

1.  Right-click on the `imicCharge.API` project in Visual Studio.
2.  Select **"Manage User Secrets"**.
3.  A `secrets.json` file will open. Populate it with your specific keys based on the template in `appsettings.Development.json`:

    ```json
    {
      "ConnectionStrings": {
        "DefaultConnection": "Your_SQL_Server_Connection_String"
      },
      "StripeSettings": {
        "SecretKey": "Your_Stripe_Secret_Key (sk_test_...)",
        "PublishableKey": "Your_Stripe_Publishable_Key (pk_test_...)",
        "WebhookSecret": "Your_Stripe_Webhook_Secret (whsec_...)"
      }
    }
    ```

### 2. Set Up the Database

1.  Open the **Package Manager Console** in Visual Studio (`Tools > NuGet Package Manager > Package Manager Console`).
2.  Ensure the "Default project" is set to `imicCharge.API`.
3.  Run the following command to apply the database migrations:

    ```powershell
    Update-Database
    ```

### 3. Run the Solution

For local development, both the API and the APP need to run simultaneously.

1.  Right-click on the Solution (`imicCharge`) in Solution Explorer.
2.  Select **"Set Startup Projects..."**.
3.  Choose **"Multiple startup projects"**.
4.  Set the **Action** for both `imicCharge.API` and `imicCharge.APP` to **"Start"**.
5.  Click OK.
6.  Press the "Start" button in Visual Studio to launch both projects.
