# SaphireSocialOSC
This software is used to get events from [saphiresocial.net](https://saphiresocial.net/) to then forwards them to VRchat via OSC.
It will only get new notifications once.

Its using C# and can be started as a small .exe to run in a termina.
## Table of Contents
- [1 Disclaimer](#1-disclaimer)
- [2 Configuration](#2-configuration)
  - [2.1 IntervalInSeconds](#21-intervalinseconds)
  - [2.2 RestConfig](#22-restconfig)
  - [2.3 OscClientConfig](#23-oscclientconfig)
  - [2.4 EventMappingConfig](#24-eventmappingconfig)
    - [2.4.1 Event Types](#241-event-types)
    - [2.4.2 Default](#242-default)
    - [2.4.3 CharacterSpecificMapping](#243-characterspecificmapping)
    - [2.4.4 ParameterConfiguration](#244-parameterconfiguration)
- [3 How to get Token](#3-how-to-get-token)
- [4 Create Exe](#4-create-exe)

## 1. Disclaimer
While iam working as a software engineer, iam not used to work in C# or windows... or making .exe's. 
There might be ways to do things better, lemme know if this is the case :)

## 2 Configuration
Configuration is done in `appsettings.json`. By default this file will be written from the same directory as the .exe is.<br>
But u can override this behavior with `--config <newPatch>` when starting the .exe.

Assuming u dont want any special setup and just use the default, u will only have to change the token of the supplied [appsettings.json](appsettings.json).<br>
See [How to get Token](#how-to-get-token)

There are multiple Parts of the config that will be explained in the following sections.

### 2.1 IntervalInSeconds
Example
```json
"IntervalInSeconds": 60,
```
This is the Interval in seconds the software is using to poll new Events from the API. The higher the value, the better the performance for u and the website itself is.
The Website does not support faster polling interval than 10 seconds

### 2.2 RestConfig
Example
```json
"RestConfig": {
    "Host": "https://saphiresocial.net/",
    "Token": "",
    "TimeoutInSeconds": 10
}
```
- **Host**: Url to saphire social.
- **Token**: Token from ur account, See [How to get Token](#how-to-get-token)
- **TimeoutInSeconds**: Timeout for API requests in seconds

### 2.3 OscClientConfig
Example
```json
"OscClientConfig": {
    "Host": "127.0.0.1",
    "Port": 9000
}
```
- Host: Host of the OSC server. By default VRChat opens it on local host.
- Port: Port of OSC server. By default VRChat uses `9000`

### 2.4 EventMappingConfig
Here we will configure what event from the API will become what event in OSC. First we have to check the possible event types
#### 2.4.1 Event Types
There are multiple event Types. U can find them in [saphiresocial](https://saphiresocial.net/developers) itself.<br>
If the link is not working, here u can find them:
- Go to character selection
- Press `API Tokens` on top right
- Press `API docs` on the top right
- Scroll down to Event Types

Currently there are following event Types, that are all added to the supplied [appsettings.json](appsettings.json):
```
dm.received
post.liked
post.commented
comment.liked
comment.replied
thread.replied
follower.new
money.received
```
#### 2.4.2 Default
Example
```json
"EventMappingConfig": {
    "Default": {
        "dm.received": { // <-- this is the event type
          "Parameter": "saphireSocial/dm/received"
        }
        //.... <here comes all the other types as well> ....
    }
}
```
In the Default settings u configre the defauls... who would have guessed...<br>
When u didn't configure any character specific parameters (see [CharacterSpecificMapping](#CharacterSpecificMapping)), it will use this configuration instead.

The supplied [appsettings.json](appsettings.json) will set a boolean value based on the event type whenever there is an incoming event.

| Event Type | Avatar Parameter |
|------------|------------------|
| `dm.received` | `saphireSocial/dm/received` |
| `post.liked` | `saphireSocial/post/liked` |
| `post.commented` | `saphireSocial/post/commented` |
| `comment.liked` | `saphireSocial/comment/liked` |
| `comment.replied` | `saphireSocial/comment/replied` |
| `thread.replied` | `saphireSocial/thread/replied` |
| `follower.new` | `saphireSocial/follower/new` |
| `money.received` | `saphireSocial/money/received` |

For more information how to configure each event type, see [ParameterConfiguration](#ParameterConfiguration)

#### 2.4.3 CharacterSpecificMapping

```json
"EventMappingConfig": {
  "Default": {...},
  "CharacterSpecificMapping": {
    "@this-is-your-character-account": {
      "post.liked": {
        "Parameter": "metagram/postLiked"
      }
      //.... <optionally here comes all other types as well> ....
    }
  }
}
```
Here u can override the default config for specific characters. This uses the character name, not display name (so ur @name).<br>
For an incoming event, it will first check if there is something configured for that character, if not it will take the default configuration.

For more information how to configure each event type, see [ParameterConfiguration](#ParameterConfiguration)

#### 2.4.4 ParameterConfiguration
```json
"dm.received": {
  "Parameter": "saphireSocial/dm/received"
    "Enabled": true,
    "Type": "bool",
    "Min": 0,
    "Max": 10,
}
```
- **Enabled**: required value. Configures the name of the parameter on the avatar used when sending the OSC message.
- **Enabled**: Optional value, defaults to `true`. Configures if events should be processed. In case of `false` they will be ignored
- **Type**: Optional value, defaults to `bool`. Possible Values are `bool`, `int`, `float`.<Br>
It defines what type of parameter it should send via OSC.
  - **bool**: Sends `true` whenever there is an event present
  - **int**: Sends the count of all events of this type.<br>
    ⚠️ Currently there is no OSC server implemented to reset this count from ur Avatar. So this value will just get bigger and bigger
  - **float**: Sends % value based on configued `Min` and `Max` and count of all events of this type. Its clamped to be inbetween 0.0 and 1.0<br>
    ⚠️ Currently there is no OSC server implemented to reset this count from ur Avatar. Meaning count will get bigger and bigger, making the value be stuck at 1.0
- **Min**: Optional value, only required when using type `float`. Its used to calculate the % using `Clamp( (Count - Min) / (Max - Min), 0f, 1f)`
- **Max**: Optional value, only required when using type `float`. Its used to calculate the % using `Clamp( (Count - Min) / (Max - Min), 0f, 1f)`

## 3 How to get Token
- Go to character selection
- Press `API Tokens` on top right
- Use "all characters" or a specific character. At the moment the Checkboxe does not matter
![docs/images/TokenConfig.png](docs/images/TokenConfig.png)
- Click on `Create token`
- ⚠️ Copy the Token. U wont be able to get it afterwards if u dont copy it

⚠️ Dont give ur Token to other People, While they cant access ur account with this, they can read ur messages/notifications⚠️ 

## 4 Create Exe
U might not trust my Exe and want to build it urself after checking my Code.
In this case u can open the terminal in the [SaphireSocialOSC](SaphireSocialOSC) where the [SaphireSocialOSC/SaphireSocialOSC.csproj](SaphireSocialOSC/SaphireSocialOSC.csproj) is and run
```terminaloutput
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```
The output exe will be in [SaphireSocialOSC/bin/Release/net10.0/win-x64/publish](SaphireSocialOSC/bin/Release/net10.0/win-x64/publish).

U might need to install .NET first. See used version in [SaphireSocialOSC.csproj](SaphireSocialOSC/SaphireSocialOSC.csproj)
