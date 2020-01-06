# <img src="/src/icon.png" height="30px"> Verify.EntityFramework

[![Build status](https://ci.appveyor.com/api/projects/status/eedjhmx5o3082tyq?svg=true)](https://ci.appveyor.com/project/SimonCropp/verify-entityframework)
[![NuGet Status](https://img.shields.io/nuget/v/Verify.EntityFramework.svg)](https://www.nuget.org/packages/Verify.EntityFramework/)

Extends [Verify](https://github.com/SimonCropp/Verify) to allow verification of web bits.


toc


## NuGet package

https://nuget.org/packages/Verify.EntityFramework/


## Usage

Enable VerifyWeb once at assembly load time:

snippet: Enable


### Controller

Given the following controller:

//snippet: MyController.cs

This test:

//snippet: MyControllerTest

Will result in the following verified file:

//snippet: MyControllerTests.Test.verified.txt



## Icon

[Spider](https://thenounproject.com/term/spider/904683/) designed by [marialuisa iborra](https://thenounproject.com/marialuisa.iborra/) from [The Noun Project](https://thenounproject.com/creativepriyanka).