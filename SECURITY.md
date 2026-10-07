# Security policy

## Reporting a problem

Open an issue or a pull request and I'll take a look. You can also report privately: open the repository's **Security** tab and choose **Report a vulnerability**.

A confirmed problem is fixed in a new release, and the advisory is published once the fix is on nuget.org. Affected versions are then marked deprecated on nuget.org with the fixed version as the alternate.

## Supported versions

Only the latest major version (2.x) gets security fixes.

## What this package is not

`ImageUrl.IsImageUrlAsync` fetches the http or https URL it is given and follows redirects; it is not a defence against server-side request forgery. A service that checks URLs from untrusted users must pass its own `HttpClient`, whose handler refuses private and loopback addresses. The obsolete `IsImageUrl` keeps 1.0.2's behaviour on purpose: it follows redirects to any host, reads local files for `file:` URLs (and rooted paths on Linux and macOS) and waits up to 100 seconds, so it must never see untrusted input. These are documented behaviours, not vulnerabilities.
