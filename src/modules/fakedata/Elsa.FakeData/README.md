# Fake Data Extension

<details>
  <summary>📖 Table of Contents</summary>
  <ol>
    <li><a href="#overview">Overview</a></li>
    <li><a href="#features">Features</a></li>
    <li>
      <a href="#getting-started">Getting Started</a>
      <ul>
        <li><a href="#prerequisites">Prerequisites</a></li>
        <li><a href="#installation">Installation</a></li>
      </ul>
    </li>
     <li>
      <a href="#configuration">Configuration</a>
      <ul>
        <li><a href="#program.cs">Program.cs</a></li>
        <li><a href="#appsettings.json">Appsettings.json</a></li>
      </ul>
    </li>
    <li><a href="#usage">Usage</a></li>
    <li><a href="#activities">Activities</a></li>
    <li><a href="#examples">Examples</a></li>
    <li><a href="#planned-features">Planned Features</a></li>
    <li><a href="#limitations">Limitations</a></li>
    <li><a href="#troubleshooting">Troubleshooting</a></li>
    <li><a href="#notes">Notes & Comments</a></li>
  </ol>
</details>

## 🧠 Overview

This package extends [Elsa Workflows](https://github.com/elsa-workflows/elsa-core) with support for **fake data**.
It introduces custom activities that make it easy to generate some fake data for testing, prototyping, or any other purpose where you might need to generate random data within your workflows.

## ✨ Key Features

- Activities: `GenerateFakeOrders`, `GenerateFakePersons`, `GenerateFakeProducts` and `GenerateFakeUsers`
- Configurable output: specify the number of items to generate
- Deterministic if needed: option to set a seed for reproducible results

---

## ⚡ Getting Started

### 📋 Prerequisites

- Elsa Workflows **3.6** or higher installed in your project.

## 🛠 Installation

The following NuGet packages are available for this extension:

```bash
Elsa.FakeData
```

You can install the clients via NuGet:

```bash
dotnet add package Elsa.FakeData
```

## ⚙️ Configuration

### Program.cs

Register the extension in your application startup:

```csharp
using Elsa.Extensions;
using Elsa.FakeData.Extensions;

services.AddElsa(elsa =>
{
    elsa.UseFakeData();
}
```

---

## 📌 Usage

Once the extension is registered with your required implementations, the activities will be ready to use, either via code or [Elsa Studio](https://github.com/elsa-workflows/elsa-studio).

## 🚀 Activities 

This extension comes with the following activities:

### GenerateFakeOrders

| Properties | Type | Description | Input/Output | Notes |
| ---------- | ---- | ----------- | --- | ----- |
| Count | int | The number of fake records to generate. | Input | Default: 10 |
| Locale | string | The locale to use when generating fake data (e.g. 'en', 'de', 'fr', 'nl') | Input | Default: en |
| Seed | int? | With a seed, each run will produce the identical data (for deterministic tests). | Input | - |

### GenerateFakePersons

| Properties | Type | Description | Input/Output | Notes |
| ---------- | ---- | ----------- | --- | ----- |
| Count | int | The number of fake records to generate. | Input | Default: 10 |
| Locale | string | The locale to use when generating fake data (e.g. 'en', 'de', 'fr', 'nl') | Input | Default: en |
| Seed | int? | With a seed, each run will produce the identical data (for deterministic tests). | Input | - |

### GenerateFakeProducts

| Properties | Type | Description | Input/Output | Notes |
| ---------- | ---- | ----------- | --- | ----- |
| Count | int | The number of fake records to generate. | Input | Default: 10 |
| Locale | string | The locale to use when generating fake data (e.g. 'en', 'de', 'fr', 'nl') | Input | Default: en |
| Seed | int? | With a seed, each run will produce the identical data (for deterministic tests). | Input | - |

### GenerateFakeUsers

| Properties | Type | Description | Input/Output | Notes |
| ---------- | ---- | ----------- | --- | ----- |
| Count | int | The number of fake records to generate. | Input | Default: 10 |
| Locale | string | The locale to use when generating fake data (e.g. 'en', 'de', 'fr', 'nl') | Input | Default: en |
| Seed | int? | With a seed, each run will produce the identical data (for deterministic tests). | Input | - |


## 🧪 Examples

### Example of your feature

```csharp
new GenerateFakeOrders
{
    Count = new Input<int>(100),
    Locale = new Input<string>("de"),
    Seed = new Input<int?>(42),
}
```

---

## 🚧 Limitations

- Supports only a predefined set of fake data types (orders, persons, products, users).
- Locale support is limited to a few languages (en, de, fr, nl), based on the `Bogus` library.

---

## 🆘 Troubleshooting

### Common Errors

No issues known at this time. If you encounter any problems, please open an issue with details about the error and your environment.

---

## 🗺️ Planned Features

No further generators planned at this time, but if you have suggestions for additional fake data types or features, please let us know!

---

## 🗒️ Notes & Comments

This extension was developed to add fake data functionality to Elsa Workflows.  
If you have ideas for improvement, encounter issues, or want to share how you're using it, feel free to open an issue or start a discussion!