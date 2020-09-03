# Project Title

CSharpOAuthConsoleApp - C# Console App to use Tradelab API to connect to leading Retail Trading terminals of Indian Broking

## Getting Started

These instructions will get you a copy of the project up and running on your local machine for development and testing purposes. See deployment for notes on how to deploy the project on a live system.

### Prerequisites

* Please refer the document http://primusapi.tradelab.in/api_tradelab
* The API uses oauth2 protocol . You will need following to get started -(Please contact your broker team to get these details)
```
appID 
appSecret
redirect_url
base_url
```
### Installing
Once you download the project ,please download following dependencied from Nuget Package Manager
```   
a) cef.redist.x64 and cef.redist.x86  version="79.1.36"
b) CefSharp.Common version="79.1.360"
c) CefSharp.WinForms" version="79.1.360"
d) RestSharp" version="106.11.3" 

Use NPM Console to type following -

Install-Package cef.redist.x86 -Version 79.1.36
Install-Package cef.redist.x64 -Version 79.1.36
Install-Package CefSharp.Common -Version 79.1.360
Install-Package CefSharp.WinForms -Version 79.1.360
Install-Package RestSharp -Version 106.11.3
```

