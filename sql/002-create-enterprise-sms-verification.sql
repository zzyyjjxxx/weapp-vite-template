CREATE TABLE IF NOT EXISTS `enterprise_sms_verification` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `creditcode` VARCHAR(64) NOT NULL,
    `mobile` VARCHAR(32) NOT NULL,
    `code` CHAR(6) NOT NULL,
    `expires_at` DATETIME NOT NULL,
    `retry_at` DATETIME NOT NULL,
    `verified_at` DATETIME NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_enterprise_sms_verification_creditcode` (`creditcode`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
