# Elsa Extensions

> New Extensions development now lives in [Elsa Core's `extensions/` directory](https://github.com/elsa-workflows/elsa-core/tree/main/extensions). Open new-development [issues](https://github.com/elsa-workflows/elsa-core/issues/new/choose) and [pull requests](https://github.com/elsa-workflows/elsa-core/pulls) in Core and follow its [contribution guide](https://github.com/elsa-workflows/elsa-core/blob/main/CONTRIBUTING.md). Existing 3.8/3.9 maintenance procedures and release branches are unchanged by this notice. Original issues, PRs and contributor history remain here; see the [source handoff register](https://github.com/elsa-workflows/elsa-core/blob/main/docs/integration-program/source-handoff-register.json).

Welcome to the **Elsa Extensions** repository! This project provides a collection of extension modules for [Elsa Workflows](https://github.com/elsa-workflows/elsa-core), enabling seamless workflow automation across various third-party services.

---

## 🚀 Extensions Status

Below is the current status of each extension. Icons indicate implementation progress:

 - ✅ = Released
 - ⏳ = Soon to be released
 - 🚧 = In development
 - 🔲 = Not started

### 📨 Messaging & Communication
| Status | Extension | Description | Module Name | Issue |
|--------|------------|-------------|-------------|-------|
| 🔲 | **Telegram** | Send & receive messages, file downloads | `Elsa.Telegram` | [Open Issue](https://github.com/elsa-workflows/elsa-core/issues/new/choose) |
| [✅]( https://github.com/elsa-workflows/elsa-extensions/tree/main/src/slack/Elsa.Slack) | **Slack** | Channel messages, user notifications | `Elsa.Slack` | [View Issue](https://github.com/elsa-workflows/elsa-extensions/issues/26) |
| 🔲 | **Discord** | Bot commands, message triggers | `Elsa.Discord` | [Open Issue](https://github.com/elsa-workflows/elsa-core/issues/new/choose) |
| 🔲 | **Microsoft Teams** | Chat automation, meeting reminders | `Elsa.Teams` | [Open Issue](https://github.com/elsa-workflows/elsa-core/issues/new/choose) |
| [✅](https://github.com/elsa-workflows/elsa-extensions/tree/main/src/telnyx/Elsa.Telnyx) | **Telnyx** | Telephony automation | `Elsa.Telnyx` | [Open Issue](https://github.com/elsa-workflows/elsa-core/issues/new/choose) |

### 📧 Email & Productivity
| Status | Extension | Description | Module Name | Issue |
|--------|------------|-------------|-------------|-------|
| 🔲 | **Gmail** | Send, receive, label, draft emails | `Elsa.Gmail` | [Open Issue](https://github.com/elsa-workflows/elsa-core/issues/new/choose) |
| 🔲 | **Outlook (Office 365)** | Email management via Microsoft Graph API | `Elsa.Outlook` | [Open Issue](https://github.com/elsa-workflows/elsa-core/issues/new/choose) |
| 🔲 | **Google Calendar** | Event scheduling and updates | `Elsa.GoogleCalendar` | [Open Issue](https://github.com/elsa-workflows/elsa-core/issues/new/choose) |
| 🔲 | **Microsoft Calendar** | Office 365 calendar extension | `Elsa.Office365Calendar` | [Open Issue](https://github.com/elsa-workflows/elsa-core/issues/new/choose) |

### 🗄️ Storage Services
| Status | Extension | Description | Module Name | Issue |
|--------|------------|-------------|-------------|-------|
| 🔲 | **Google Drive** | Upload, download, manage files | `Elsa.GoogleDrive` | [Open Issue](https://github.com/elsa-workflows/elsa-core/issues/new/choose) |
| 🔲 | **OneDrive** | File storage and access for Office 365 | `Elsa.OneDrive` | [Open Issue](https://github.com/elsa-workflows/elsa-core/issues/new/choose) |
| 🚧 | **Azure Storage** | Blob storage management | `Elsa.AzureStorage` | [View Issue](https://github.com/elsa-workflows/elsa-extensions/issues/1) |
| 🔲 | **Dropbox** | Cloud storage and file sync | `Elsa.Dropbox` | [Open Issue](https://github.com/elsa-workflows/elsa-core/issues/new/choose) |

### 🗃️ Databases (SQL)
| Status | Extension | Description | Module Name | Issue |
|--------|------------|-------------|-------------|-------|
| [✅]( https://github.com/elsa-workflows/elsa-extensions/tree/main/src/sql/README.md) | **MySql** | Connect and query MySQL databases | `Elsa.Sql.MySql` | [Open Issue](https://github.com/elsa-workflows/elsa-core/issues/new/choose) |
| [✅]( https://github.com/elsa-workflows/elsa-extensions/tree/main/src/sql/README.md) | **PostgreSql** | Connect and query PostgreSql databases | `Elsa.Sql.PostgreSql` | [Open Issue](https://github.com/elsa-workflows/elsa-core/issues/new/choose) |
| [✅]( https://github.com/elsa-workflows/elsa-extensions/tree/main/src/sql/README.md) | **Sqlite** | Connect and query Sqlite databases | `Elsa.Sql.Sqlite` | [Open Issue](https://github.com/elsa-workflows/elsa-core/issues/new/choose) |
| [✅]( https://github.com/elsa-workflows/elsa-extensions/tree/main/src/sql/README.md) | **Sql Server** | Connect and query Microsoft SQL Server databases | `Elsa.Sql.SqlServer` | [Open Issue](https://github.com/elsa-workflows/elsa-core/issues/new/choose) |

### 🛠 DevOps & Monitoring
| Status | Extension | Description | Module Name | Issue |
|--------|------------|-------------|-------------|-------|
| 🔲 | **Azure DevOps** | Pipelines, repositories, work items | `Elsa.AzureDevOps` | [Open Issue](https://github.com/elsa-workflows/elsa-core/issues/new/choose) |
| [✅](https://github.com/elsa-workflows/elsa-extensions/tree/main/src/devops/Elsa.DevOps.GitHub/README.md) | **GitHub** | PR automation, repo events | `Elsa.DevOps.GitHub` | [Fixed Issue](https://github.com/elsa-workflows/elsa-extensions/issues/6) |
| 🔲 | **GitLab** | CI/CD triggers and repo management | `Elsa.GitLab` | [Open Issue](https://github.com/elsa-workflows/elsa-core/issues/new/choose) |
| 🔲 | **Jenkins** | Pipeline automation and job execution | `Elsa.Jenkins` | [Open Issue](https://github.com/elsa-workflows/elsa-core/issues/new/choose) |
| 🔲 | **Datadog** | Monitoring, logging, and alerts | `Elsa.Datadog` | [Open Issue](https://github.com/elsa-workflows/elsa-core/issues/new/choose) |

### ☁️ Cloud Compute & Serverless
| Status | Extension | Description | Module Name | Issue |
|--------|------------|-------------|-------------|-------|
| 🔲 | **Azure Functions** | Serverless function triggers | `Elsa.AzureFunctions` | [Open Issue](https://github.com/elsa-workflows/elsa-core/issues/new/choose) |
| 🔲 | **AWS Lambda** | Invoke and trigger Lambda functions | `Elsa.AWSLambda` | [Open Issue](https://github.com/elsa-workflows/elsa-core/issues/new/choose) |
| 🔲 | **Google Cloud Functions** | Event-driven function automation | `Elsa.GoogleCloudFunctions` | [Open Issue](https://github.com/elsa-workflows/elsa-core/issues/new/choose) |

### 📊 CRM & Sales Automation
| Status | Extension | Description | Module Name | Issue |
|--------|------------|-------------|-------------|-------|
| 🔲 | **Salesforce** | Lead management, opportunity tracking | `Elsa.Salesforce` | [Open Issue](https://github.com/elsa-workflows/elsa-core/issues/new/choose) |
| 🔲 | **HubSpot** | Contacts, deals, email automation | `Elsa.HubSpot` | [Open Issue](https://github.com/elsa-workflows/elsa-core/issues/new/choose) |
| 🔲 | **Zoho CRM** | Lead scoring, campaign tracking | `Elsa.ZohoCRM` | [Open Issue](https://github.com/elsa-workflows/elsa-core/issues/new/choose) |
| 🔲 | **Pipedrive** | Sales pipeline automation | `Elsa.Pipedrive` | [Open Issue](https://github.com/elsa-workflows/elsa-core/issues/new/choose) |

### 💰 Payments & Finance
| Status | Extension | Description | Module Name | Issue |
|--------|------------|-------------|-------------|-------|
| 🔲 | **Stripe** | Payment processing, subscriptions | `Elsa.Stripe` | [Open Issue](https://github.com/elsa-workflows/elsa-core/issues/new/choose) |
| 🔲 | **PayPal** | Transactions, refunds, invoicing | `Elsa.PayPal` | [Open Issue](https://github.com/elsa-workflows/elsa-core/issues/new/choose) |
| 🔲 | **Square** | POS and e-commerce transactions | `Elsa.Square` | [Open Issue](https://github.com/elsa-workflows/elsa-core/issues/new/choose) |
| 🔲 | **QuickBooks** | Invoice and expense automation | `Elsa.QuickBooks` | [Open Issue](https://github.com/elsa-workflows/elsa-core/issues/new/choose) |

### 🤖 AI & Automation
| Status | Extension | Description | Module Name | Issue |
|--------|------------|-------------|-------------|-------|
| 🔲 | **OpenAI** | GPT-based text generation, chatbots | `Elsa.OpenAI` | [Open Issue](https://github.com/elsa-workflows/elsa-core/issues/new/choose) |
| 🔲 | **Google AI** | AI-enhanced search, translation | `Elsa.GoogleAI` | [Open Issue](https://github.com/elsa-workflows/elsa-core/issues/new/choose) |
| 🔲 | **AWS Comprehend** | NLP services for text analysis | `Elsa.AWSComprehend` | [Open Issue](https://github.com/elsa-workflows/elsa-core/issues/new/choose) |
| 🔲 | **Azure AI** | Vision, speech, language processing | `Elsa.AzureAI` | [Open Issue](https://github.com/elsa-workflows/elsa-core/issues/new/choose) |

### 🎥 Video & Streaming Platforms
| Status | Extension | Description | Module Name | Issue |
|--------|------------|-------------|-------------|-------|
| 🔲 | **YouTube** | Upload videos, retrieve video details, manage playlists | `Elsa.YouTube` | [Open Issue](https://github.com/elsa-workflows/elsa-core/issues/new/choose)|
| 🔲 | **Rumble** | Upload videos, fetch channel videos, manage content | `Elsa.Rumble` | [Open Issue](https://github.com/elsa-workflows/elsa-core/issues/new/choose) |
| 🔲 | **Twitch** | Live stream events, chat automation, clip management | `Elsa.Twitch` | [Open Issue](https://github.com/elsa-workflows/elsa-core/issues/new/choose) |
| 🔲 | **Vimeo** | Upload videos, manage privacy settings, get video analytics | `Elsa.Vimeo` | [Open Issue](https://github.com/elsa-workflows/elsa-core/issues/new/choose) |

### 🏭 Industrial Communication Protocols
| Status | Extension | Description | Module Name | Issue |
|--------|------------|-------------|-------------|-------|
| 🔲 | **OPC UA** | Browse nodes, Read/Write values, Subscribe/Unsubscribe nodes | `Elsa.OPC.UA` | [Open Issue](https://github.com/elsa-workflows/elsa-core/issues/new/choose) |
| 🔲 | **Modbus** | Read/Write coils, Read discrete Inputs, Read/Write registers | `Elsa.Modbus` | [Open Issue](https://github.com/elsa-workflows/elsa-core/issues/new/choose) |
| 🔲 | **MQTT Sparkplug** | Discover and Subscribe to topics, Publish messages | `Elsa.MQTT.Sparkplug` | [Open Issue](https://github.com/elsa-workflows/elsa-core/issues/new/choose) |

---

## 📦 Structure
Each Extension is structured as a standalone package under the `Elsa` namespace. Example:


```plaintext
...
├─azure/
│   ├── Elsa.AzureServiceBus/
│   │   ├── README.md
│   │   ├── Services/
│   │   ├── Activities/
│   │   ├── AI/
│   │   └── ...
│   └── Elsa.AzureStorage/
│       ├── README.md
│       ├── Services/
│       ├── Activities/
│       ├── AI/
│       └── ...
├─sql/
│   ├── README.md
│   ├── Elsa.Sql/
│   │   ├── Services/
│   │   ├── Activities/
│   │   ├── AI/
│   │   └── ...
│   └── Elsa.Sql.Sqlite/
│       ├── Services/
│       ├── Activities/
│       ├── AI/
│       └── ...
...
```

## ⚡ Getting Started
To install a specific extension:
```sh
dotnet add package Elsa.Gmail
```
To enable it in Elsa Workflows:
```csharp
services.AddElsa(elsa => elsa.AddGmail());
```

## 🔥 Contributing
We welcome contributions! See our [Contributing Guide](CONTRIBUTING.md) for more details.

## 📜 License
This repository is licensed under the [MIT License](https://github.com/elsa-workflows/elsa-extensions/blob/main/LICENSE).
