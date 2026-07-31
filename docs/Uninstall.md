# Rock Detective Uninstall Guide

The package ships full uninstall support (`uninstall/deletefile.lst` plus migration `Down()` methods), but Rock's package manager does not yet execute plugin down-migrations, so a clean removal is file deletion plus a short SQL pass.

## 1. Remove the files

Via the package manager's uninstall if your Rock version supports it, or manually:

```
RockWeb\Bin\com.yesuchum.RockDetective.dll
RockWeb\Plugins\com_yesuchum\RockDetective\   (whole folder)
```

## 2. Remove the data

Run against the Rock database. Order matters; children first.

```sql
DELETE FROM [PluginMigration] WHERE [PluginAssemblyName] LIKE 'com.yesuchum.RockDetective%';

DROP TABLE IF EXISTS [_com_yesuchum_RockDetective_CaseStatement];
DROP TABLE IF EXISTS [_com_yesuchum_RockDetective_Experiment];
DROP TABLE IF EXISTS [_com_yesuchum_RockDetective_Hypothesis];
DROP TABLE IF EXISTS [_com_yesuchum_RockDetective_CaseEntry];
DROP TABLE IF EXISTS [_com_yesuchum_RockDetective_CaseParticipant];
DROP TABLE IF EXISTS [_com_yesuchum_RockDetective_CaseFile];
```

## 3. Remove the Rock configuration

All of this is optional housekeeping; none of it breaks anything if left behind.

- Pages: Admin Tools > Installed Plugins > Rock Detective (delete children first: Case Board, Witness Statement).
- Block types: the three under the "Rock Detective" category.
- Security role: the **Rock Detective Users** group.
- System communications: the three titled "Rock Detective ..." under Communications > System Communications.
- Jobs: **Rock Detective Witness Reminders** and **Rock Detective Follow-Up Checks**.
- Entity types: rows named `com.yesuchum.RockDetective.*` clean themselves up on the next restart once the DLL is gone, or delete them manually.
- Categories: the four seeded Case File categories (Software Bug, Performance, Process, Ministry Operations).

## 4. Restart

Restart the Rock application and confirm the Installed Plugins page no longer lists Rock Detective and the exception log stays quiet.
