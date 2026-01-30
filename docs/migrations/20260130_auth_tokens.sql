-- AuthSession / RefreshToken tables for SQLite
-- Generated on 2026-01-30

CREATE TABLE IF NOT EXISTS AuthSession (
    Id INTEGER PRIMARY KEY,
    TenantIdValue TEXT NOT NULL,
    UserId INTEGER NOT NULL,
    ClientType TEXT NOT NULL,
    ClientPlatform TEXT NOT NULL,
    ClientChannel TEXT NOT NULL,
    ClientAgent TEXT NOT NULL,
    IpAddress TEXT NOT NULL,
    UserAgent TEXT NOT NULL,
    CreatedAt TEXT NOT NULL,
    LastSeenAt TEXT NOT NULL,
    ExpiresAt TEXT NOT NULL,
    RevokedAt TEXT NULL
);

CREATE INDEX IF NOT EXISTS IX_AuthSession_Tenant_User
    ON AuthSession (TenantIdValue, UserId);

CREATE TABLE IF NOT EXISTS RefreshToken (
    Id INTEGER PRIMARY KEY,
    TenantIdValue TEXT NOT NULL,
    UserId INTEGER NOT NULL,
    SessionId INTEGER NOT NULL,
    TokenHash TEXT NOT NULL,
    IssuedAt TEXT NOT NULL,
    ExpiresAt TEXT NOT NULL,
    RevokedAt TEXT NULL,
    ReplacedById INTEGER NULL
);

CREATE INDEX IF NOT EXISTS IX_RefreshToken_Tenant_TokenHash
    ON RefreshToken (TenantIdValue, TokenHash);

CREATE INDEX IF NOT EXISTS IX_RefreshToken_Tenant_Session
    ON RefreshToken (TenantIdValue, SessionId);
