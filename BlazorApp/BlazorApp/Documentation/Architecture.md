# BlazorApp Architecture

## Purpose
`BlazorApp` is the ASP.NET Core host for the web/editor experience. It configures the HTTP pipeline, interactive render modes, static assets and service registrations needed to serve the client UI.

## What Belongs Here
- ASP.NET Core startup and middleware
- Server-side DI registrations needed for prerendering and hosting
- Host-level configuration, environment setup, and routing

## What Does Not Belong Here
- Native rendering code
- Ownership of shared engine/domain models (move to `3DEngine.Core`)
- Blazor page implementation details that belong in `BlazorApp.Client`

## Why `BlazorApp` référence `BlazorApp.Client`
`BlazorApp` reference `BlazorApp.Client` (via a ProjectReference) to expose client-side routable components/pages to the server. This server:
- discovers routable components (`@page`) located in the referenced assemblies ;
- serves static assets (scripts, styles, DLLs) via `MapStaticAssets()` ;
- maps the root component via `MapRazorComponents<App>()` ; `<Routes />` in `App.razor` then renders the client project pages.

This approach allows:
- hosting/serving a Blazor "client" UI while maintaining the possibility of prerendering and server integration ;
- centralizing HTTP pipeline configuration on the host side.

## HeadOutlet, Routes and RenderMode
`App.razor` often uses: