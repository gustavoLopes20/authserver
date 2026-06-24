CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;

ALTER DATABASE CHARACTER SET utf8mb4;

CREATE TABLE `AspNetRoles` (
    `Id` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Name` varchar(256) CHARACTER SET utf8mb4 NULL,
    `NormalizedName` varchar(256) CHARACTER SET utf8mb4 NULL,
    `ConcurrencyStamp` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_AspNetRoles` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `AspNetUsers` (
    `Id` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Role` varchar(20) CHARACTER SET utf8mb4 NULL,
    `ExternalId` varchar(12) CHARACTER SET utf8mb4 NULL,
    `EmailAlternativo` varchar(64) CHARACTER SET utf8mb4 NULL,
    `UserName` varchar(256) CHARACTER SET utf8mb4 NULL,
    `NormalizedUserName` varchar(256) CHARACTER SET utf8mb4 NULL,
    `Email` varchar(256) CHARACTER SET utf8mb4 NULL,
    `NormalizedEmail` varchar(256) CHARACTER SET utf8mb4 NULL,
    `EmailConfirmed` tinyint(1) NOT NULL,
    `PasswordHash` longtext CHARACTER SET utf8mb4 NULL,
    `SecurityStamp` longtext CHARACTER SET utf8mb4 NULL,
    `ConcurrencyStamp` longtext CHARACTER SET utf8mb4 NULL,
    `PhoneNumber` longtext CHARACTER SET utf8mb4 NULL,
    `PhoneNumberConfirmed` tinyint(1) NOT NULL,
    `TwoFactorEnabled` tinyint(1) NOT NULL,
    `LockoutEnd` datetime(6) NULL,
    `LockoutEnabled` tinyint(1) NOT NULL,
    `AccessFailedCount` int NOT NULL,
    CONSTRAINT `PK_AspNetUsers` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `OpenIddictApplications` (
    `Id` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Permissions` longtext CHARACTER SET utf8mb4 NULL,
    `ApplicationType` varchar(50) CHARACTER SET utf8mb4 NULL,
    `ClientId` varchar(100) CHARACTER SET utf8mb4 NULL,
    `ClientSecret` longtext CHARACTER SET utf8mb4 NULL,
    `ClientType` varchar(50) CHARACTER SET utf8mb4 NULL,
    `ConcurrencyToken` varchar(50) CHARACTER SET utf8mb4 NULL,
    `ConsentType` varchar(50) CHARACTER SET utf8mb4 NULL,
    `DisplayName` longtext CHARACTER SET utf8mb4 NULL,
    `DisplayNames` longtext CHARACTER SET utf8mb4 NULL,
    `JsonWebKeySet` longtext CHARACTER SET utf8mb4 NULL,
    `Permissions1` longtext CHARACTER SET utf8mb4 NULL,
    `PostLogoutRedirectUris` longtext CHARACTER SET utf8mb4 NULL,
    `Properties` longtext CHARACTER SET utf8mb4 NULL,
    `RedirectUris` longtext CHARACTER SET utf8mb4 NULL,
    `Requirements` longtext CHARACTER SET utf8mb4 NULL,
    `Settings` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_OpenIddictApplications` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `OpenIddictScopes` (
    `Id` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `ConcurrencyToken` varchar(50) CHARACTER SET utf8mb4 NULL,
    `Description` longtext CHARACTER SET utf8mb4 NULL,
    `Descriptions` longtext CHARACTER SET utf8mb4 NULL,
    `DisplayName` longtext CHARACTER SET utf8mb4 NULL,
    `DisplayNames` longtext CHARACTER SET utf8mb4 NULL,
    `Name` varchar(200) CHARACTER SET utf8mb4 NULL,
    `Properties` longtext CHARACTER SET utf8mb4 NULL,
    `Resources` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_OpenIddictScopes` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `AspNetRoleClaims` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `RoleId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `ClaimType` longtext CHARACTER SET utf8mb4 NULL,
    `ClaimValue` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_AspNetRoleClaims` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_AspNetRoleClaims_AspNetRoles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `AspNetRoles` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `AspNetUserClaims` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `UserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `ClaimType` longtext CHARACTER SET utf8mb4 NULL,
    `ClaimValue` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_AspNetUserClaims` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_AspNetUserClaims_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `AspNetUserLogins` (
    `LoginProvider` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `ProviderKey` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `ProviderDisplayName` longtext CHARACTER SET utf8mb4 NULL,
    `UserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_AspNetUserLogins` PRIMARY KEY (`LoginProvider`, `ProviderKey`),
    CONSTRAINT `FK_AspNetUserLogins_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `AspNetUserRoles` (
    `UserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `RoleId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_AspNetUserRoles` PRIMARY KEY (`UserId`, `RoleId`),
    CONSTRAINT `FK_AspNetUserRoles_AspNetRoles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `AspNetRoles` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_AspNetUserRoles_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `AspNetUserTokens` (
    `UserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `LoginProvider` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Name` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Value` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_AspNetUserTokens` PRIMARY KEY (`UserId`, `LoginProvider`, `Name`),
    CONSTRAINT `FK_AspNetUserTokens_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `UserApplications` (
    `UserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `ClientId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_UserApplications` PRIMARY KEY (`UserId`, `ClientId`),
    CONSTRAINT `FK_UserApplications_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `OpenIddictAuthorizations` (
    `Id` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `ApplicationId` varchar(255) CHARACTER SET utf8mb4 NULL,
    `ConcurrencyToken` varchar(50) CHARACTER SET utf8mb4 NULL,
    `CreationDate` datetime(6) NULL,
    `Properties` longtext CHARACTER SET utf8mb4 NULL,
    `Scopes` longtext CHARACTER SET utf8mb4 NULL,
    `Status` varchar(50) CHARACTER SET utf8mb4 NULL,
    `Subject` varchar(400) CHARACTER SET utf8mb4 NULL,
    `Type` varchar(50) CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_OpenIddictAuthorizations` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_OpenIddictAuthorizations_OpenIddictApplications_ApplicationId` FOREIGN KEY (`ApplicationId`) REFERENCES `OpenIddictApplications` (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `OpenIddictTokens` (
    `Id` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `ApplicationId` varchar(255) CHARACTER SET utf8mb4 NULL,
    `AuthorizationId` varchar(255) CHARACTER SET utf8mb4 NULL,
    `ConcurrencyToken` varchar(50) CHARACTER SET utf8mb4 NULL,
    `CreationDate` datetime(6) NULL,
    `ExpirationDate` datetime(6) NULL,
    `Payload` longtext CHARACTER SET utf8mb4 NULL,
    `Properties` longtext CHARACTER SET utf8mb4 NULL,
    `RedemptionDate` datetime(6) NULL,
    `ReferenceId` varchar(100) CHARACTER SET utf8mb4 NULL,
    `Status` varchar(50) CHARACTER SET utf8mb4 NULL,
    `Subject` varchar(400) CHARACTER SET utf8mb4 NULL,
    `Type` varchar(50) CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_OpenIddictTokens` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_OpenIddictTokens_OpenIddictApplications_ApplicationId` FOREIGN KEY (`ApplicationId`) REFERENCES `OpenIddictApplications` (`Id`),
    CONSTRAINT `FK_OpenIddictTokens_OpenIddictAuthorizations_AuthorizationId` FOREIGN KEY (`AuthorizationId`) REFERENCES `OpenIddictAuthorizations` (`Id`)
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_AspNetRoleClaims_RoleId` ON `AspNetRoleClaims` (`RoleId`);

CREATE UNIQUE INDEX `RoleNameIndex` ON `AspNetRoles` (`NormalizedName`);

CREATE INDEX `IX_AspNetUserClaims_UserId` ON `AspNetUserClaims` (`UserId`);

CREATE INDEX `IX_AspNetUserLogins_UserId` ON `AspNetUserLogins` (`UserId`);

CREATE INDEX `IX_AspNetUserRoles_RoleId` ON `AspNetUserRoles` (`RoleId`);

CREATE INDEX `EmailIndex` ON `AspNetUsers` (`NormalizedEmail`);

CREATE UNIQUE INDEX `UserNameIndex` ON `AspNetUsers` (`NormalizedUserName`);

CREATE UNIQUE INDEX `IX_OpenIddictApplications_ClientId` ON `OpenIddictApplications` (`ClientId`);

CREATE INDEX `IX_OpenIddictAuthorizations_ApplicationId_Status_Subject_Type` ON `OpenIddictAuthorizations` (`ApplicationId`, `Status`, `Subject`, `Type`);

CREATE UNIQUE INDEX `IX_OpenIddictScopes_Name` ON `OpenIddictScopes` (`Name`);

CREATE INDEX `IX_OpenIddictTokens_ApplicationId_Status_Subject_Type` ON `OpenIddictTokens` (`ApplicationId`, `Status`, `Subject`, `Type`);

CREATE INDEX `IX_OpenIddictTokens_AuthorizationId` ON `OpenIddictTokens` (`AuthorizationId`);

CREATE UNIQUE INDEX `IX_OpenIddictTokens_ReferenceId` ON `OpenIddictTokens` (`ReferenceId`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260623234908_AdicionarControleMultiApp', '8.0.11');

COMMIT;

