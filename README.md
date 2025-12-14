# Elsa Integrations

Welcome to the **Elsa Integrations** repository! This project provides a collection of integration modules for [Elsa Workflows](https://github.com/elsa-workflows/elsa-core), enabling seamless workflow automation across various third-party services.

---

## 🚀 Integration Status

Below is the current status of each integration. Checkboxes indicate implementation progress.

### 📨 Messaging & Communication
| Status | Integration | Description | Module Name | Issue |
|--------|------------|-------------|-------------|-------|
| [ ] | **Telegram** | Send & receive messages, file downloads | `Elsa.Telegram` |  |
| [x] | **Slack** | Channel messages, user notifications | `Elsa.Slack` | |
| [ ] | **Discord** | Bot commands, message triggers | `Elsa.Discord` | |
| [ ] | **Microsoft Teams** | Chat automation, meeting reminders | `Elsa.Teams` | |
| [x] | **Telnyx** | Telephony automation | `Elsa.Telnyx` | |

### 📧 Email & Productivity
| Status | Integration | Description | Module Name | Issue |
|--------|------------|-------------|-------------|-------|
| [ ] | **Gmail** | Send, receive, label, draft emails | `Elsa.Gmail` | |
| [ ] | **Outlook (Office 365)** | Email management via Microsoft Graph API | `Elsa.Outlook` | |
| [ ] | **Google Calendar** | Event scheduling and updates | `Elsa.GoogleCalendar` | |
| [ ] | **Microsoft Calendar** | Office 365 calendar integration | `Elsa.Office365Calendar` | |

### 🗄️ Storage Services
| Status | Integration | Description | Module Name | Issue |
|--------|------------|-------------|-------------|-------|
| [ ] | **Google Drive** | Upload, download, manage files | `Elsa.GoogleDrive` | |
| [ ] | **OneDrive** | File storage and access for Office 365 | `Elsa.OneDrive` | |
| [ ] | **Azure Storage** | Blob storage management | `Elsa.AzureStorage` | https://github.com/elsa-workflows/elsa-integrations/issues/1 |
| [ ] | **Dropbox** | Cloud storage and file sync | `Elsa.Dropbox` | |

### 🛠 DevOps & Monitoring
| Status | Integration | Description | Module Name | Issue |
|--------|------------|-------------|-------------|-------|
| [ ] | **Azure DevOps** | Pipelines, repositories, work items | `Elsa.AzureDevOps` | |
| [ ] | **GitHub** | PR automation, repo events | `Elsa.GitHub` | |
| [ ] | **GitLab** | CI/CD triggers and repo management | `Elsa.GitLab` | |
| [ ] | **Jenkins** | Pipeline automation and job execution | `Elsa.Jenkins` | |
| [ ] | **Datadog** | Monitoring, logging, and alerts | `Elsa.Datadog` | |

### ☁️ Cloud Compute & Serverless
| Status | Integration | Description | Module Name | Issue |
|--------|------------|-------------|-------------|-------|
| [ ] | **Azure Functions** | Serverless function triggers | `Elsa.AzureFunctions` | |
| [ ] | **AWS Lambda** | Invoke and trigger Lambda functions | `Elsa.AWSLambda` | |
| [ ] | **Google Cloud Functions** | Event-driven function automation | `Elsa.GoogleCloudFunctions` | |

### 📊 CRM & Sales Automation
| Status | Integration | Description | Module Name | Issue |
|--------|------------|-------------|-------------|-------|
| [ ] | **Salesforce** | Lead management, opportunity tracking | `Elsa.Salesforce` | [Open Issue](#) |
| [ ] | **HubSpot** | Contacts, deals, email automation | `Elsa.HubSpot` | [Open Issue](#) |
| [ ] | **Zoho CRM** | Lead scoring, campaign tracking | `Elsa.ZohoCRM` | [Open Issue](#) |
| [ ] | **Pipedrive** | Sales pipeline automation | `Elsa.Pipedrive` | [Open Issue](#) |

### 💰 Payments & Finance
| Status | Integration | Description | Module Name | Issue |
|--------|------------|-------------|-------------|-------|
| [ ] | **Stripe** | Payment processing, subscriptions | `Elsa.Stripe` | |
| [ ] | **PayPal** | Transactions, refunds, invoicing | `Elsa.PayPal` | |
| [ ] | **Square** | POS and e-commerce transactions | `Elsa.Square` | |
| [ ] | **QuickBooks** | Invoice and expense automation | `Elsa.QuickBooks` | |

### 🤖 AI & Automation
| Status | Integration | Description | Module Name | Issue |
|--------|------------|-------------|-------------|-------|
| [x] | **OpenAI** | GPT-based text generation, chatbots | `Elsa.OpenAI` | |
| [ ] | **Google AI** | AI-enhanced search, translation | `Elsa.GoogleAI` | |
| [ ] | **AWS Comprehend** | NLP services for text analysis | `Elsa.AWSComprehend` | |
| [ ] | **Azure AI** | Vision, speech, language processing | `Elsa.AzureAI` | |

### 🎥 Video & Streaming Platforms
| Status | Integration | Description | Module Name | Issue |
|--------|------------|-------------|-------------|-------|
| [ ] | **YouTube** | Upload videos, retrieve video details, manage playlists | `Elsa.YouTube` | [Open Issue](#) |
| [ ] | **Rumble** | Upload videos, fetch channel videos, manage content | `Elsa.Rumble` | [Open Issue](#) |
| [ ] | **Twitch** | Live stream events, chat automation, clip management | `Elsa.Twitch` | [Open Issue](#) |
| [ ] | **Vimeo** | Upload videos, manage privacy settings, get video analytics | `Elsa.Vimeo` | [Open Issue](#) |

### 🏭 Industrial Communication Protocols
| Status | Integration | Description | Module Name | Issue |
|--------|------------|-------------|-------------|-------|
| [ ] | **OPC UA** | Browse nodes, Read/Write values, Subscribe/Unsubscribe nodes | `Elsa.OPC.UA` | |
| [ ] | **Modbus** | Read/Write coils, Read discrete Inputs, Read/Write registers | `Elsa.Modbus` | |
| [ ] | **MQTT Sparkplug** | Discover and Subscribe to topics, Publish messages | `Elsa.MQTT.Sparkplug` | |
---

## 📦 Structure
Each integration is structured as a standalone package under the `Elsa` namespace. Example:

```
Elsa.Gmail/
  ├── Services/
  ├── Activities/
  ├── AI/
Elsa.Telegram/
  ├── Services/
  ├── Activities/
  ├── AI/
```

## ⚡ Getting Started
To install a specific integration:
```sh
dotnet add package Elsa.Gmail
```
To enable it in Elsa Workflows:
```csharp
services.AddElsa()
        .AddGmailIntegration();
```

## 🔥 Contributing
We welcome contributions! See our [Contributing Guide](CONTRIBUTING.md) for more details.

## 📜 License
This repository is licensed under the [MIT License](https://github.com/elsa-workflows/elsa-integrations/blob/main/LICENSE).
