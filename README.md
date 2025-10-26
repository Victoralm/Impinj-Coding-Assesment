# Impinj Coding Assesment


## Assesment directions

```
TIME:            2-3 hours
LANGUAGES:       C# preferred
FRAMEWORKS:      NET or NET CORE preferred,
TEST FRAMEWORKS: MS BUILD/NUNIT/XUNIT preferred
TYPE:            Web Api
FILES:           In repository
TESTS:           preferred
SUBMIT:          ZipFile, Github Repository Link
```

## Overview:
This exercise is to asses a candidates ability to design and implement a solution to a problem in a given period of time.

We want to see a demonstration of your thought process, skills and development experiences.

This solution should be able to run locally.

_Be prepared to explain your design decisions and architecture!_


## Problem:
Create a web API that can parse a sales record file into an object. The API should return an object with the following fields:
* The median Unit Cost
* The most common Region
* The first and last Order Date and the days between them
* The total revenue


## Included:
* Sample input at `Input\SalesRecords.csv`
* A git ignore file for a C# project
* A git attributes file

<br></br>

_Hint: a web api skeleton can be created with `dotnet new webapi`_

<br></br>

## Solution Description

### API

A Minimal API project built with .NET 9, designed to remain lightweight and highly performant.

- Implements the CQRS pattern to separate read and write operations.
- Includes input validation to ensure data integrity.
- Applies the Service pattern to decouple business logic from the API endpoints.
- Incorporates Resilience patterns to ensure reliability and fault tolerance during potential server failures.
- Implements a Logging pattern to monitor and trace the overall behavior of the API.

### Tests

Implements xUnit tests to validate every property and behavior of the returned objects, ensuring correctness and reliability.
