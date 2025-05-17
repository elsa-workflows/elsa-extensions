# Contributing to Elsa Extensions

Thank you for your interest in contributing to **Elsa Extensions**! 🎉  
We welcome contributions from the community to enhance workflow automation by adding new integrations, improving existing ones, fixing bugs, or refining documentation.

---

## 🚀 How to Contribute

### 1️⃣ Fork the Repository  
Click the **Fork** button on the top right of this repository and clone it to your local machine:  
```sh
git clone https://github.com/YOUR-USERNAME/elsa-extensions.git
cd elsa-extensions
```

### 2️⃣ Set Up the Development Environment  
Ensure you have the following installed:
- .NET 8 or higher SDK  
- A text editor or IDE (Visual Studio, Rider, VS Code)  
- Docker (if working on extensions that require local services)

Then restore dependencies:
```sh
dotnet restore
```

### 3️⃣ Choose an Issue or Feature  
- Check our [Issues](https://github.com/elsa-workflows/elsa-extensions/issues) for **"good first issue"** labels.  
- If you have a new idea, open an issue to discuss before starting development.

### 4️⃣ Develop Your Changes  
Create a new branch for your changes:
```sh
git checkout -b feature/extension-name
```
Follow the repository's structure when adding a new extensions:  
```
[group-name]/
  └── Elsa.[extension-name]/
      ├── Services/
      ├── Activities/
      ├── AI/
      ...
```

### 5️⃣ Commit & Push  
Ensure your code follows the project’s conventions and add meaningful commit messages:
```sh
git add .
git commit -m "Add support for [extension-name]"
git push origin feature/extensions-name
```

### 6️⃣ Create a Pull Request (PR)  
Go to your fork on GitHub and open a PR to merge your branch into `main`.  

Please include:
- A **clear description** of your changes.  
- **Relevant issue numbers**, if applicable.  
- Screenshots/logs (if UI/functional changes).  

Our team will review your PR, provide feedback if necessary, and merge it once approved! ✅  

---

## 🛠 Contribution Guidelines

- **Code Style**: Follow `.editorconfig` settings and existing project conventions.  
- **Documentation**: Every extension must have a `README.md` at the root of its group folder or project. See [README-TEMPLATE](https://github.com/elsa-workflows/elsa-extensions/blob/main/README-TEMPLATE.md). 
- **Tests**: If possible, add unit/integration tests for your changes.  

---

## 💬 Need Help?  
If you need assistance, feel free to ask in [Discussions](https://github.com/elsa-workflows/elsa-extensions/discussions) or open an issue.

🚀 Happy coding and thank you for contributing to Elsa Extensions!  
