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

## Overview

This exercise is to asses a candidates ability to design and implement a solution to a problem in a given period of time.

We want to see a demonstration of your thought process, skills and development experiences.

This solution should be able to run locally.

_Be prepared to explain your design decisions and architecture!_

## Problem

Create a web API that can parse a sales record file into an object. The API should return an object with the following fields:

* The median Unit Cost
* The most common Region
* The first and last Order Date and the days between them
* The total revenue

## Included

* Sample input at `Input\SalesRecords.csv`
* A git ignore file for a C# project
* A git attributes file

<br></br>

_Hint: a web api skeleton can be created with `dotnet new webapi`_

<br></br>

## Solution Description

### API

A Minimal API project built with .NET 9, designed to remain lightweight and highly performant.

* Implements the CQRS pattern to separate read and write operations.
* Includes input validation to ensure data integrity.
* Applies the Service pattern to decouple business logic from the API endpoints.
* Incorporates Resilience patterns to ensure reliability and fault tolerance during potential server failures.
* Implements a Logging pattern to monitor and trace the overall behavior of the API.

### Tests

Implements xUnit tests to validate every property and behavior of the returned objects, ensuring correctness and reliability.

<br></br>

## Personal Notes

### PriorityQueue vs QuickSelect

In my local tests, both approaches produced equivalent response times.
Further investigation should focus on verifying memory-consumption parity between both methods.

NOTE: While inspecting the Process Memory in Visual Studio, I got the impression that the GC handles the PriorityQueue version more efficiently.

PriorityQueue:

```bash
2025-10-27 12:08:23.976 -03:00 [INF] Execution attempt. Source: 'default/(null)/Retry', Operation Key: 'null', Result: 'PreScreen_API.Models.SalesSummaryDto', Handled: 'false', Attempt: '0', Execution Time: 553.1677ms
2025-10-27 12:08:26.836 -03:00 [INF] Execution attempt. Source: 'default/(null)/Retry', Operation Key: 'null', Result: 'PreScreen_API.Models.SalesSummaryDto', Handled: 'false', Attempt: '0', Execution Time: 505.1935ms
2025-10-27 12:08:28.026 -03:00 [INF] Execution attempt. Source: 'default/(null)/Retry', Operation Key: 'null', Result: 'PreScreen_API.Models.SalesSummaryDto', Handled: 'false', Attempt: '0', Execution Time: 217.3273ms
```

QuickSelect:

```bash
2025-10-27 13:12:03.115 -03:00 [INF] Execution attempt. Source: 'default/(null)/Retry', Operation Key: 'null', Result: 'PreScreen_API.Models.SalesSummaryDto', Handled: 'false', Attempt: '0', Execution Time: 468.8407ms
2025-10-27 13:12:06.636 -03:00 [INF] Execution attempt. Source: 'default/(null)/Retry', Operation Key: 'null', Result: 'PreScreen_API.Models.SalesSummaryDto', Handled: 'false', Attempt: '0', Execution Time: 539.2537ms
2025-10-27 13:12:08.117 -03:00 [INF] Execution attempt. Source: 'default/(null)/Retry', Operation Key: 'null', Result: 'PreScreen_API.Models.SalesSummaryDto', Handled: 'false', Attempt: '0', Execution Time: 246.7871ms
```
