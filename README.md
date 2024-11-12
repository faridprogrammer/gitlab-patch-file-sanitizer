# Patch File Sanitizer

A C# console application to monitor a specified folder for `.patch` files and sanitize them by removing sensitive information, such as GitLab namespaces, commit hashes, committer names, commit messages, email addresses.

## Features

- Monitors a specified folder for `.patch` files.
- Automatically removes sensitive information from each detected `.patch` file, including:
  - Git paths and namespaces.
  - Commit hashes.
  - Email addresses.
  - Commits and commit messages.
  - File paths.
- Avoids reprocessing files immediately after they are saved to prevent duplicate events.

## Requirements

- .NET Core SDK 3.1 or higher.

## Installation

1. **Clone the Repository**:
   ```bash
   git clone https://github.com/faridprogrammer/Gitlab-Patch-File-Sanitizer.git
   ```

2. **Navigate to the Project Directory**:
   ```bash
   cd yourrepository
   ```

3. **Build the Program**:
   ```bash
   dotnet build
   ```

## Usage

1. **Run the Program**:
   You can specify the folder to monitor as a command-line argument:
   ```bash
   dotnet run <folder_path_to_watch>
   ```

   Replace `<folder_path_to_watch>` with the actual path of the folder you want to monitor for `.patch` files.

2. **Sanitizing Files**:
   The program will automatically sanitize each `.patch` file placed in the specified folder, removing sensitive information and saving the cleaned content back to the file. The following sensitive information will be removed:
   - Git paths/namespaces.
   - Commit hashes (40-character hexadecimal strings).
   - Email addresses.
   - File paths in Windows or Unix format.
   - Committer names.
   - Commit messages.

3. **Stopping the Program**:
   Press `q` in the console to quit the application.

## Example

```bash
dotnet run "C:\path\to\watch\folder"
```

## How It Works

The program uses the `FileSystemWatcher` to monitor the specified folder for new or modified `.patch` files. When a `.patch` file is detected, it performs the following sanitization steps:

1. **Removes Git paths and namespaces** – Removes typical GitLab paths and nested namespaces.
2. **Removes commit hashes** – Replaces 40-character hexadecimal commit hashes with `"REMOVED_HASH"`.
3. **Removes email addresses** – Replaces any detected email addresses with `"REMOVED_EMAIL"`.
4. **Removes file system paths** – Replaces Windows and Unix-style file paths with `"REMOVED_PATH"`.
5. **Removes committer name** – Replaces the committer name found in lines starting with "From:" with `"REMOVED_COMMITTER"`.
6. **Removes commit messages** – Replaces commit messages (starting after a line containing "Date:") with `"REMOVED_COMMIT_MESSAGE"`.

## Contributing

Feel free to submit issues or contribute improvements to this program by submitting a pull request.

## License

This project is licensed under the MIT License
