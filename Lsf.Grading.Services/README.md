# Grading Services
This is a service which notifies about updated grades

## Usage
The service reads from `appsettings.json` or environment.
Alternatively a `.json` file can be provided using the `LSF_CONFIGFILE_PATH` environment variable.

`json`-key|Environment Variable|default|desc
---|---|---|---
`UserName`| `LSF_USER` | | Username for lsf|
`Password`| `LSF_PASSWORD` | | Password for lsf |
`LoginCookie`| | | Cookie obtained from logging in in the web or `CLI` |
`TelegramBotAccessToken` |  `LSF_TELEGRAM_BOT_TOKEN` | | Access token as obtained from @botfather |
`TelegramChatId`| `LSF_TELEGRAM_CHAT_ID` | | Chat id of the recipient of update messages |
`SaveFile`| | `gradingresults.json` | Path to file to store the grades (used for persistent diffs over restarts) |
`BaseUrl`| | `https://lsf.ovgu.de`| Baseurl of the lsf instance

## Docker
Build the docker image from the repository root directory with
```
docker build --file Dockerfiles/Lsf.Grading.Services --tag "lsfgrading" .
```

Having a configuration file at `/lsf/settings.json` start the container with
```
docker run -it --rm -e LSF_CONFIGFILE_PATH="/data/settings.json" -v "/lsf/:/data" lsfgrading
```