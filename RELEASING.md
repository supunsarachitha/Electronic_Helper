# Releasing to Google Play

How to build a signed bundle and what to enter in Play Console. Written for version 2.0.0, the first release
since 1.9 in 2018.

## Before the first upload

Two things need sorting out in Play Console before a 2.0.0 bundle can go anywhere.

### 1. Find out why the listing is not public

On 10 October 2026 neither `lk.stechbuzz.electronic` (the ID the app has always shipped with) nor
`lk.stechbuzz.electronichelper` (the ID the old README linked to) had a public page on Google Play. Open
[Play Console](https://play.google.com/console) and check the app's status and the **Policy status** page.

- If the app was hidden for targeting an old API level, uploading 2.0.0 fixes it.
- If it was removed for a policy violation, read the notice. Version 2.0.0 removes the likely causes (links to
  unauthorised e-books and images copied from other sites), but you may need to submit an appeal.
- If the app is not in your account at all, create a new app and use `lk.stechbuzz.electronic` as its package
  name.

### 2. Find the signing key

An update must be signed with the same upload key as the version before it. Version 1.9 was signed with
`supun.keystore`, which the old project file placed at:

```
C:\Users\sliyanaarachchi\AppData\Local\Xamarin\Mono for Android\Keystore\supun\supun.keystore
```

- **You have the keystore and its passwords:** use it as described below.
- **You have lost it, and the app uses Play App Signing:** ask Google for an upload key reset under
  **Setup → App signing → Request upload key reset**. It takes a few days.
- **You have lost it, and the app does not use Play App Signing:** the existing listing cannot be updated. The
  app has to be published as a new app under a new package name, which means changing `ApplicationId` in
  [ElectronicHelper.csproj](src/ElectronicHelper/ElectronicHelper.csproj).

Keep the keystore out of the repository. `.gitignore` already excludes `*.keystore` and `*.jks`.

## Build a signed bundle

1. Raise the version in [ElectronicHelper.csproj](src/ElectronicHelper/ElectronicHelper.csproj).
   `ApplicationVersion` is the version code and must be higher than every earlier upload: 1.9 used 10, and
   2.0.0 uses 11. `ApplicationDisplayVersion` is the name users see.

2. Run the tests.

   ```bash
   dotnet test
   ```

3. Build, giving the keystore through environment variables so that no secret is written to disk.

   ```bash
   export ANDROID_KEYSTORE_PATH=/path/to/supun.keystore
   export ANDROID_KEY_ALIAS=your-key-alias
   export ANDROID_KEYSTORE_PASSWORD='...'
   export ANDROID_KEY_PASSWORD='...'

   dotnet build src/ElectronicHelper -c Release
   ```

   The bundle to upload is:

   ```
   src/ElectronicHelper/bin/Release/net10.0-android/lk.stechbuzz.electronic-Signed.aab
   ```

   Without those variables the build still succeeds, but signs with the debug key. Play rejects that.

4. Install the matching APK on a real phone and try it before uploading.

   ```bash
   adb install -r src/ElectronicHelper/bin/Release/net10.0-android/lk.stechbuzz.electronic-Signed.apk
   ```

## What the build already satisfies

Checked against the 2.0.0 release build on 10 October 2026.

| Play requirement | Status |
|---|---|
| Targets Android 16 (API 36), required for updates since 31 August 2026 | `targetSdkVersion` is 36 |
| 64-bit native code | Ships `arm64-v8a` and `x86_64`, plus 32-bit `armeabi-v7a` for older phones |
| Android App Bundle format | Builds an `.aab` |
| Native libraries aligned for 16 KB memory pages | All 45 libraries per ABI are aligned to 16 KB |
| Permissions | Requests none |
| Edge-to-edge display, enforced when targeting API 35 and later | Every screen handles system bar and keyboard insets |
| Predictive back, enforced when targeting API 36 | No screen intercepts Back |
| Large screens: no fixed orientation or aspect ratio | Nothing is locked; the home screen uses more columns on wide displays |

## Play Console declarations

Under **Policy → App content**. These answers are true of version 2.0.0. Revisit them if the app ever gains
network access, adverts, analytics or accounts.

| Section | Answer |
|---|---|
| Privacy policy | `https://supunsarachitha.github.io/Electronic_Helper/privacy.html` |
| Ads | No, the app does not contain ads |
| App access | All functionality is available without special access |
| Content rating | Complete the questionnaire as a reference or utility app with no violence, sexual content, profanity, drugs, gambling or user interaction. The expected rating is Everyone / PEGI 3 |
| Target audience | 13 and over. Choosing a younger age group brings the Families policy into play, which this app has not been reviewed against |
| Data safety | "Does your app collect or share any of the required user data types?" **No**. The app has no network permission. Feedback goes through the user's own e-mail app, which Play does not count as collection by this app |
| Advertising ID | No, the app does not use an advertising ID |
| News app, health, financial features, government app | No to each |

The privacy policy URL only works once GitHub Pages is switched on: see below.

## Publish the website

The site in [docs/](docs/) is plain HTML and needs no build step.

1. Merge the release branch into `master`.
2. On GitHub, open **Settings → Pages**.
3. Under **Build and deployment**, choose **Deploy from a branch**, then `master` and `/docs`.
4. After a minute, check that `https://supunsarachitha.github.io/Electronic_Helper/privacy.html` loads.

Do this before submitting to Play: the review checks that the privacy policy link works.

## Roll out

1. Upload the bundle to the **Internal testing** track first and install it from Play on your own phone.
2. Read the **Pre-launch report**, which runs the app on real devices and flags crashes and accessibility
   problems.
3. Promote to **Production** with a staged rollout, such as 20%, and widen it once the crash rate looks normal.

Listing text, the icon, the feature graphic and screenshots are in [store/](store/).

## After release

- Tag the commit: `git tag v2.0.0 && git push origin v2.0.0`.
- Move the changelog entry from "unreleased" to the release date.
- Google raises the required target API level every August. Plan to rebuild on the next .NET release each
  year, or the app will again become impossible to update.
