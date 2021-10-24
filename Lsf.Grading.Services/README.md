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
`SaveFile`| | `gradingresults.json` | Path to file to store the grades (used for persistent diffs over restarts) |
`BaseUrl`| | `https://lsf.ovgu.de`| Baseurl of the lsf instance

### Notifiers
There are multiple ways this service can notify. To use them add their configuration section to the `settings.json`.
Add a toplevel object with their corresponding `sectionkey`

####Telegram
Sends a message to a specified chat using a telegram bot.

`sectionkey: Telegram`

`json`-key|Environment Variable|desc
---|---|---
`TelegramBotAccessToken` |  `LSF_TELEGRAM_BOT_TOKEN` | Access token as obtained from @botfather
`TelegramChatId`| `LSF_TELEGRAM_CHAT_ID` | Chat id of the recipient of update messages

####Callback url
Sends updates and errors via request to a given url. 

`sectionkey: Callback`

`json`-key|Default|desc
---|---|---
`CallbackUrl` |  | Url to call
`Method` | `GET` | Methode to use

## Systemd
This service can be used with `systemd`. See [LsfGradingService.service](LsfGradingService.service) for an example file.

## Docker
Build the docker image from the repository root directory with
```
docker build --file Dockerfiles/Lsf.Grading.Services --tag "lsfgrading" .
```

Having a configuration file at `/lsf/settings.json` start the container with
```
docker run -it --rm -e LSF_CONFIGFILE_PATH="/data/settings.json" -v "/lsf/:/data" lsfgrading
```