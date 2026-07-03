# Inscura's Jellyfin Metadata Plugin

Languages: **English** | [简体中文](docs/README-zh.md) | [日本語](docs/README-ja.md) | [한국어](docs/README-ko.md)

Inscura is a local media library application that organizes information such as movie details, cast, genres, cover art, background images, and trailers. Jellyfin handles playback and media library management, but it is not aware of the data that Inscura has already organized.

This plugin connects Inscura’s local API service to Jellyfin. When Jellyfin refreshes movie metadata, the plugin writes data such as titles, descriptions, release dates, ratings, genres, production companies, cast, cover art, background images, and thumbnails to Jellyfin.

The plugin only reads Inscura’s media library data and the image resources generated within the library; it does not download, move, rename, delete, or modify the original media files.

## Current Capabilities

- Prioritizes matching based on the actual file path and filename provided by Jellyfin to reduce mis-matches caused by manually modified Jellyfin titles.
- If no path match is found, it will continue to attempt matching based on the identifier in the filename and the Jellyfin title.
- Supports writing to Jellyfin-compatible movie metadata fields, including title, original title, synopsis, release date, year, rating, genre, studio, country, tags, cast, director, screenwriter, producer, poster, background image, and thumbnail.
- Supports importing local image resources from Inscura as candidates for Jellyfin’s Primary, Backdrop, Thumb, Banner, Logo, Art, and Disc images.
- Remote trailers currently only import YouTube URLs. **For non-YouTube local or online trailers, please export them using the NFO feature in the Inscura app and submit them to Jellyfin for scanning.**

## Enabling the Inscura Local API Service

1. Open Inscura and navigate to the media library you want to sync with Jellyfin.
2. Go to the API settings in Settings and enable the local API service.
3. If the API service uses token-based authentication, save the API token displayed on the settings page.
4. On the device hosting the Jellyfin server, access the health check URL to verify that the service is reachable.

Example:

```bash
curl "http://[ip]:28687/api/v1/health"
```

If Jellyfin and Inscura are not running on the same machine, do not enter `127.0.0.1` as the service address in the plugin. Instead, enter the local network address of the computer running Inscura, for example:

```text
http://[ip]:28687
```

The local API service runs for the duration of the current media library’s lifecycle: it listens on the port while the media library is open and the service is enabled, and **stops when the media library is locked, closed, or the application exits.**

## Recommended Installation Method: Plugin Repository

With this method, Jellyfin reads the `manifest.json` from the plugin repository, making installation and subsequent upgrades more convenient. For users in mainland China, we recommend using the jsDelivr URL.

Repository Name:

```text
Inscura
```

Repository URL:

```text
https://cdn.jsdelivr.net/gh/InscuraApp/inscura-jellyfin-plugin@release/manifest.json
```

Installation Steps:

1. Go to the Jellyfin console.
2. Open the Plugins directory or the Plugins settings page.
3. Go to Repositories.
4. Click Add.
5. Enter `Inscura` as the name.
6. Enter the jsDelivr manifest URL listed above.
7. After saving, go to the Plugins directory or Catalog.
8. Locate `Inscura` and install it.
9. Restart Jellyfin.

If your network has stable access to GitHub, you can also use the manifest file from the `release` branch; however, for users in Mainland China, we recommend using the jsDelivr URL provided above.

## Alternative Installation Method: Directly Download the ZIP File

If you cannot install the plugin via the Jellyfin plugin repository, you can manually download the plugin ZIP file and place it in the Jellyfin plugin directory.

### Obtain the Download Link

Open the manifest file:

```text
https://cdn.jsdelivr.net/gh/InscuraApp/inscura-jellyfin-plugin@release/manifest.json
```

Copy the latest version of `sourceUrl` from the list and download the corresponding ZIP file.

The current release URL format is similar to:

```text
https://cdn.jsdelivr.net/gh/InscuraApp/inscura-jellyfin-plugin@release/releases/Inscura_[version].zip
```

### Placing the Plugin File

Locate the Jellyfin data directory and navigate to the `plugins` directory within it. Common locations are listed below; if your system configuration differs, refer to the data directory displayed in the Jellyfin console or the actual mapping path used during deployment.

| Environment | Common Plugin Directory |
| --- | --- |
| Linux Package Installation | `/var/lib/jellyfin/plugins/` |
| Docker | Directory mapped from the host machine to the `/config/plugins/` container |
| Windows Service Installation | `%ProgramData%\Jellyfin\Server\plugins\` |
| Windows Portable or User Mode | `plugins\` under the Jellyfin data directory |

Manual Installation Steps:

1. Stop Jellyfin, or be prepared to restart Jellyfin after the copy is complete.
2. Create a version directory under the `plugins` directory, for example:

```text
plugins/Inscura_0.1.0.0/
```

3. Unzip the file and place its contents in that directory.
4. The directory only needs to contain the plugin DLL:

```text
Jellyfin.Plugin.Inscura.dll
```

5. Verify that the Jellyfin process has read permission for this file.
6. Restart Jellyfin.
7. Go to the Plugins page in the console and confirm that `Inscura` has been loaded.

## Enabling the Plugin in Jellyfin

After installing the plugin, you’ll also need to enable the Inscura metadata source in your movie library.

1. Go to the Jellyfin console.
2. Open the Media Library and edit your movie library.
3. Enable `Inscura` in the Metadata Downloader.
4. We recommend placing `Inscura` ahead of other movie metadata sources.
5. Enable `Inscura` in the Image Downloader.
6. Save the media library settings.
7. Go to the Plugins settings page and enter the Inscura API URL and API token.
8. Refresh the metadata or identify the movies.

When using this for the first time, it is recommended to first select a small number of movies to refresh their metadata. After confirming that the titles, cast, cover art, and background images are as expected, you can then refresh the entire library in bulk.

## Plugin Settings Guide

| Setting | Description |
| --- | --- |
| Inscura API URL | The address of the Inscura local API service. You must enter the LAN address if Jellyfin and Inscura are not running on the same machine |
| API Token | Enter this when the local API service uses token authentication; leave blank if Inscura is set to "No Authentication" |
| Search result limit | The number of candidates requested from Inscura per match |
| Request timeout | Timeout for requests to the Inscura local API |
| Enable movie metadata provider | Enable or disable movie metadata scraping |
| Enable image provider | Enable or disable image import |
| Import YouTube trailers | When enabled, only import remote YouTube trailers |
| Use Inscura preview images as Thumb candidates | Use Inscura preview images as candidates for Jellyfin Thumb images |
| Use gallery images as Backdrop candidates | Use Inscura gallery images as candidates for Jellyfin Backdrop images |

## Recommendations

- When using this for the first time, refresh a small number of movies first to ensure the matching results meet your expectations before refreshing in bulk.
- If movie titles in Jellyfin have been manually changed, the plugin will still prioritize matching based on the actual file path and filename, rather than relying solely on the title.
- If the Inscura service address or token has been changed, you must update it in the Jellyfin plugin settings and then refresh the metadata.
- If the Inscura media library is locked or disabled, Jellyfin will be unable to read the metadata.
- Do not rely on the plugin to import non-YouTube trailers; instead, use the NFO feature in the Inscura app to export them for Jellyfin to scan.

## Troubleshooting

### Jellyfin Cannot Detect the Inscura Plugin

1. If you installed via a repository, verify that the repository URL is accessible from the Jellyfin server.
2. If you installed manually, verify that the plugin files are located in the `plugins/Inscura_[version]/` directory within the Jellyfin data directory.
3. Verify that `Jellyfin.Plugin.Inscura.dll` exists in the plugin directory.
4. Verify that the Jellyfin process has read permission for this file.
5. Restart Jellyfin.
6. Reopen the Jellyfin web interface and check the Plugins page in the console.

### Jellyfin Can See the Plugin but Has No Metadata

1. On the device hosting the Jellyfin server, access the Inscura health check URL.
2. Verify that the Inscura media library is enabled and that the local API service is running.
3. Verify that the Inscura API URL in the plugin is not an invalid `127.0.0.1`.
4. If the API uses token-based authentication, verify that the correct token is entered in the plugin.
5. Verify that `Inscura` is enabled in the movie library’s metadata downloader.
6. Refresh the movie’s metadata, selecting “Overwrite all metadata” as the refresh mode.

### Cover Art, Background Images, or Cast Portraits Not Displaying

1. Verify that the Jellyfin server can access the Inscura API URL.
2. If the API uses token authentication, verify that the plugin’s token is correct.
3. Verify that the corresponding media or cast member in Inscura actually has available image resources.
4. Verify that `Inscura` is enabled in the movie library’s image downloader.
5. Refresh the movie’s metadata and, if necessary, select “Replace existing images.”

### Trailers Are Not Imported

1. Verify that `Import YouTube trailers` is enabled in the plugin settings.
2. Verify that the trailer URL in Inscura is a YouTube URL.
3. The current plugin does not import non-YouTube local or online trailers.
4. To import non-YouTube trailers, export them using the NFO feature in the Inscura app and submit them to Jellyfin for scanning.

## Upgrading the Plugin

### Upgrading via the Plugin Repository

1. Verify that the plugin repository URL is still accessible.
2. Go to the Plugins page in the Jellyfin console.
3. Check if an update is available for `Inscura`.
4. Install the update.
5. Restart Jellyfin.

### Manual Upgrade

1. Stop Jellyfin.
2. Download the new version ZIP file.
3. Create a new version directory under the `plugins` directory, for example, `Inscura_0.1.1.0`.
4. Extract the new version ZIP file into that directory.
5. Keep the old version directory until you confirm that the new version loads correctly.
6. Start Jellyfin.
7. Once you confirm that the new version is working properly, delete the old version directory.

If the Jellyfin web interface still displays old settings or metadata after the upgrade, first force-refresh the browser page, then reopen the plugin settings or refresh the movie metadata.
