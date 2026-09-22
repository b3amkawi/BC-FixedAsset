# UI parity map — deployed Site to ASP.NET Web Forms

The v3.1 Web Forms presentation layer is rebuilt from the currently deployed BC Fixed Asset Site design system.

| Site element | Web Forms implementation |
| --- | --- |
| Two-column sign-in | `Account/Login.aspx` + `.login-shell` |
| 278 px gradient sidebar | `Site.Master` + `.sidebar` |
| 76 px sticky top bar | `Site.Master` + `.topbar` |
| Better Code logo | Same PNG bytes in `Assets/better-code-logo.png` |
| Application cards | `Portal/Default.aspx` + `.application-card` |
| Dashboard metrics/workflow | `FixedAsset/Dashboard.aspx` |
| Search toolbar and asset table | `FixedAsset/Survey/List.aspx` |
| Thumbnail/status/row actions | `.asset-thumb`, `.status-pill`, `.survey-actions` |
| Survey detail dialog | `pnlDetail` + `.modal-layer`/`.modal-card` |
| Survey editor dialog | `FixedAsset/Survey/Edit.aspx` + `.survey-editor` |
| Three image upload cards | `.image-upload-grid`/`.image-field` |
| Loading dialog | `Site.Master` + `.loading-dialog` |
| Profile cards and avatar | `Account/Profile.aspx` |
| Administration tabs | `Admin/*.aspx` + `.admin-tabs` |
| Reports cards | `FixedAsset/Reports.aspx` |
| Mobile breakpoints | 1100 px, 820 px and 560 px in `Content/site.css` |

The business layer remains native .NET Framework 4.8 with Web Forms postbacks; presentation dimensions, colors, spacing, card/table/dialog styling and responsive breakpoints are synchronized with the Site source.
