# Note Bookmark

![GitHub Release](https://img.shields.io/github/v/release/fboucher/NoteBookmark)  ![.NET](https://img.shields.io/badge/10.0-512BD4?logo=dotnet&logoColor=fff)  ![MAUI](https://img.shields.io/badge/MAUI-10.0-gray?logo=dotnet&logoColor=white&labelColor=512BD4)  ![Android Supported](https://img.shields.io/badge/Android-supported-3DDC84?logo=android&logoColor=white)  [![Unit Tests](https://github.com/FBoucher/NoteBookmark/actions/workflows/running-unit-tests.yml/badge.svg)](https://github.com/FBoucher/NoteBookmark/actions/workflows/running-unit-tests.yml)
[![OpenAI Compatible](https://img.shields.io/badge/OpenAI-Compatible-10a37f?logo=data%3Aimage%2Fsvg%2Bxml%3Bbase64%2CPHN2ZyByb2xlPSJpbWciIHZpZXdCb3g9IjAgMCAyNCAyNCIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj48dGl0bGU%2BT3BlbkFJPC90aXRsZT48cGF0aCBmaWxsPSJ3aGl0ZSIgZD0iTTIyLjI4MTkgOS44MjExYTUuOTg0NyA1Ljk4NDcgMCAwIDAtLjUxNTctNC45MTA4IDYuMDQ2MiA2LjA0NjIgMCAwIDAtNi41MDk4LTIuOUE2LjA2NTEgNi4wNjUxIDAgMCAwIDQuOTgwNyA0LjE4MThhNS45ODQ3IDUuOTg0NyAwIDAgMC0zLjk5NzcgMi45IDYuMDQ2MiA2LjA0NjIgMCAwIDAgLjc0MjcgNy4wOTY2IDUuOTggNS45OCAwIDAgMCAuNTExIDQuOTEwNyA2LjA1MSA2LjA1MSAwIDAgMCA2LjUxNDYgMi45MDAxQTUuOTg0NyA1Ljk4NDcgMCAwIDAgMTMuMjU5OSAyNGE2LjA1NTcgNi4wNTU3IDAgMCAwIDUuNzcxOC00LjIwNTggNS45ODk0IDUuOTg5NCAwIDAgMCAzLjk5NzctMi45MDAxIDYuMDU1NyA2LjA1NTcgMCAwIDAtLjc0NzUtNy4wNzI5em0tOS4wMjIgMTIuNjA4MWE0LjQ3NTUgNC40NzU1IDAgMCAxLTIuODc2NC0xLjA0MDhsLjE0MTktLjA4MDQgNC43NzgzLTIuNzU4MmEuNzk0OC43OTQ4IDAgMCAwIC4zOTI3LS42ODEzdi02LjczNjlsMi4wMiAxLjE2ODZhLjA3MS4wNzEgMCAwIDEgLjAzOC4wNTJ2NS41ODI2YTQuNTA0IDQuNTA0IDAgMCAxLTQuNDk0NSA0LjQ5NDR6bS05LjY2MDctNC4xMjU0YTQuNDcwOCA0LjQ3MDggMCAwIDEtLjUzNDYtMy4wMTM3bC4xNDIuMDg1MiA0Ljc4MyAyLjc1ODJhLjc3MTIuNzcxMiAwIDAgMCAuNzgwNiAwbDUuODQyOC0zLjM2ODV2Mi4zMzI0YS4wODA0LjA4MDQgMCAwIDEtLjAzMzIuMDYxNUw5Ljc0IDE5Ljk1MDJhNC40OTkyIDQuNDk5MiAwIDAgMS02LjE0MDgtMS42NDY0ek0yLjM0MDggNy44OTU2YTQuNDg1IDQuNDg1IDAgMCAxIDIuMzY1NS0xLjk3MjhWMTEuNmEuNzY2NC43NjY0IDAgMCAwIC4zODc5LjY3NjVsNS44MTQ0IDMuMzU0My0yLjAyMDEgMS4xNjg1YS4wNzU3LjA3NTcgMCAwIDEtLjA3MSAwbC00LjgzMDMtMi43ODY1QTQuNTA0IDQuNTA0IDAgMCAxIDIuMzQwOCA3Ljg3MnptMTYuNTk2MyAzLjg1NThMMTMuMTAzOCA4LjM2NCAxNS4xMTkyIDcuMmEuMDc1Ny4wNzU3IDAgMCAxIC4wNzEgMGw0LjgzMDMgMi43OTEzYTQuNDk0NCA0LjQ5NDQgMCAwIDEtLjY3NjUgOC4xMDQydi01LjY3NzJhLjc5Ljc5IDAgMCAwLS40MDctLjY2N3ptMi4wMTA3LTMuMDIzMWwtLjE0Mi0uMDg1Mi00Ljc3MzUtMi43ODE4YS43NzU5Ljc3NTkgMCAwIDAtLjc4NTQgMEw5LjQwOSA5LjIyOTdWNi44OTc0YS4wNjYyLjA2NjIgMCAwIDEgLjAyODQtLjA2MTVsNC44MzAzLTIuNzg2NmE0LjQ5OTIgNC40OTkyIDAgMCAxIDYuNjgwMiA0LjY2ek04LjMwNjUgMTIuODYzbC0yLjAyLTEuMTYzOGEuMDgwNC4wODA0IDAgMCAxLS4wMzgtLjA1NjdWNi4wNzQyYTQuNDk5MiA0LjQ5OTIgMCAwIDEgNy4zNzU3LTMuNDUzN2wtLjE0Mi4wODA1TDguNzA0IDUuNDU5YS43OTQ4Ljc5NDggMCAwIDAtLjM5MjcuNjgxM3ptMS4wOTc2LTIuMzY1NGwyLjYwMi0xLjQ5OTggMi42MDY5IDEuNDk5OHYyLjk5OTRsLTIuNTk3NCAxLjQ5OTctMi42MDY3LTEuNDk5N1oiLz48L3N2Zz4%3D)](https://developers.openai.com/)




I use this project mostly everyday. I build it to help me collecting my thoughts about articles, and blob posts I read during the week and then aggregate them in a #ReadingNotes blog post. You can find those post on my blog [here](https://frankysnotes.com).

NoteBookmark is composed of three main sections:

- **Post**: where you can manage a posts "to read", and add notes to them.
- **Generate Summary**: where you can generate a summary of the posts you read.
- **Summaries**: where you can see all the summaries you generated.

![Slide show of all NoteBookmark Screens](gh/images/NoteBookmark-Tour_hd.gif)

## Run Options

- Development: running the Aspire project is the easiest path and everything is wired automatically.
- Production-style: run with containers and deploy to Azure.

Run locally with Aspire:

```bash
dotnet run --project src/NoteBookmark.AppHost
```

## How to deploy Your own NoteBookmark

### Get the code on your machine

- Fork this repository to your account.
- Clone the repository to your local machine.


### Deploy the solution (5 mins)

Using Azure Developer CLI let's initialize your environment. In a terminal, at the root of the project, run the following command. When ask give it a name (ex: NoteBookmark-dev).

```bash
azd init
```

Now let's deploy the solution. Run the following command in the terminal. You will have to select your Azure subscription where you want to deploy the solution, and a location (ex: eastus).

```bash
azd up
```

It should take around five minutes to deploy the solution. Once it's done, you will see the URL for **Deploying service blazor-app**.

### Secure the App in a few clicks

The app is now deployed, but it's not secure. Navigate to the Azure Portal, and find the Resource Group you just deployed (ex: rg-notebookmark-dev). In this resource group, open the Container App **Container App**. From the left menu, select **Authentication** and click the **Add identity provider**.

You can choose between multiple providers, I like to use Microsoft since it's deploy in Azure and I'm already logged in. If Microsoft is choose, select the recomended **Client secret expiration** (ex: 180 days). You can keep all the other default settings. Click **Add**.

Next time you will navigate to the app, you will be prompt a to login with your Microsoft account. The first time you will have a **Permissions requested** screen, click **Accept**.

Voila! Your app is now secure.

## Documentation

For detailed setup guides and configuration information:
- [Keycloak Container Setup](/docs/keycloak-container-setup.md) - Start a local Keycloak instance if you do not already have one
- [Keycloak Authentication Setup](/docs/keycloak-setup.md) - Complete guide for setting up Keycloak authentication
- [Docker Compose Deployment](/docs/docker-compose-deployment.md) - Deploy NoteBookmark containers (assumes a healthy Keycloak + configured realm)

## Contributing

Your contributions are welcome! Take a look at [CONTRIBUTING](/CONTRIBUTING.md) for details.
