-- MySQL dump 10.13  Distrib 8.0.46, for Linux (x86_64)
--
-- Host: localhost    Database: koperasi
-- ------------------------------------------------------
-- Server version	8.0.46

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `audit_logs`
--

DROP TABLE IF EXISTS `audit_logs`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `audit_logs` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `actor_type` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `actor_id` bigint unsigned DEFAULT NULL,
  `action` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `auditable_type` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `auditable_id` bigint unsigned DEFAULT NULL,
  `changes` json DEFAULT NULL,
  `ip_address` varchar(45) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `created_at` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `audit_logs_actor_type_actor_id_index` (`actor_type`,`actor_id`),
  KEY `audit_logs_auditable_type_auditable_id_index` (`auditable_type`,`auditable_id`),
  KEY `audit_logs_action_created_at_index` (`action`,`created_at`),
  KEY `audit_logs_created_idx` (`created_at`),
  KEY `audit_logs_actor_created_idx` (`actor_id`,`created_at`),
  KEY `audit_logs_actor_type_id_idx` (`actor_type`,`actor_id`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `audit_logs`
--

LOCK TABLES `audit_logs` WRITE;
/*!40000 ALTER TABLE `audit_logs` DISABLE KEYS */;
INSERT INTO `audit_logs` VALUES (1,'staff',1,'void','transaction',9,NULL,NULL,'2026-10-20 09:46:07');
/*!40000 ALTER TABLE `audit_logs` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `cache`
--

DROP TABLE IF EXISTS `cache`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `cache` (
  `key` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `value` mediumtext COLLATE utf8mb4_unicode_ci NOT NULL,
  `expiration` int NOT NULL,
  PRIMARY KEY (`key`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `cache`
--

LOCK TABLES `cache` WRITE;
/*!40000 ALTER TABLE `cache` DISABLE KEYS */;
/*!40000 ALTER TABLE `cache` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `cache_locks`
--

DROP TABLE IF EXISTS `cache_locks`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `cache_locks` (
  `key` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `owner` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `expiration` int NOT NULL,
  PRIMARY KEY (`key`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `cache_locks`
--

LOCK TABLES `cache_locks` WRITE;
/*!40000 ALTER TABLE `cache_locks` DISABLE KEYS */;
/*!40000 ALTER TABLE `cache_locks` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `discounts`
--

DROP TABLE IF EXISTS `discounts`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `discounts` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `name` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `description` text COLLATE utf8mb4_unicode_ci,
  `type` enum('percentage','flat','buy_x_get_y') COLLATE utf8mb4_unicode_ci NOT NULL,
  `value` decimal(12,2) NOT NULL DEFAULT '0.00',
  `buy_qty` int DEFAULT NULL,
  `get_qty` int DEFAULT NULL,
  `scope` enum('general','member') COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'general',
  `category_id` bigint unsigned DEFAULT NULL,
  `min_purchase` decimal(12,2) DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL DEFAULT '1',
  `starts_at` timestamp NULL DEFAULT NULL,
  `ends_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `discounts_category_id_foreign` (`category_id`),
  KEY `discounts_is_active_scope_starts_at_ends_at_index` (`is_active`,`scope`,`starts_at`,`ends_at`),
  CONSTRAINT `discounts_category_id_foreign` FOREIGN KEY (`category_id`) REFERENCES `item_categories` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `discounts`
--

LOCK TABLES `discounts` WRITE;
/*!40000 ALTER TABLE `discounts` DISABLE KEYS */;
INSERT INTO `discounts` VALUES (1,'Diskon Kemerdekaan 10%','Diskon umum 10% untuk semua pelanggan','percentage',10.00,NULL,NULL,'general',NULL,NULL,1,'2026-10-17 16:45:17','2026-11-16 16:45:17','2026-10-20 16:45:17','2026-10-20 16:45:17'),(2,'Diskon Anggota 5%','Diskon eksklusif anggota koperasi','percentage',5.00,NULL,NULL,'member',NULL,NULL,1,NULL,NULL,'2026-10-20 16:45:17','2026-10-20 16:45:17'),(3,'Beli 2 Gratis 1 - Kopi Sachet','Buy 2 get 1 free','buy_x_get_y',0.00,2,1,'general',NULL,NULL,1,NULL,NULL,'2026-10-20 16:45:17','2026-10-20 16:45:17');
/*!40000 ALTER TABLE `discounts` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `failed_jobs`
--

DROP TABLE IF EXISTS `failed_jobs`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `failed_jobs` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `uuid` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `connection` text COLLATE utf8mb4_unicode_ci NOT NULL,
  `queue` text COLLATE utf8mb4_unicode_ci NOT NULL,
  `payload` longtext COLLATE utf8mb4_unicode_ci NOT NULL,
  `exception` longtext COLLATE utf8mb4_unicode_ci NOT NULL,
  `failed_at` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `failed_jobs_uuid_unique` (`uuid`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `failed_jobs`
--

LOCK TABLES `failed_jobs` WRITE;
/*!40000 ALTER TABLE `failed_jobs` DISABLE KEYS */;
/*!40000 ALTER TABLE `failed_jobs` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `item_categories`
--

DROP TABLE IF EXISTS `item_categories`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `item_categories` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `name` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `description` text COLLATE utf8mb4_unicode_ci,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `item_categories_name_unique` (`name`)
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `item_categories`
--

LOCK TABLES `item_categories` WRITE;
/*!40000 ALTER TABLE `item_categories` DISABLE KEYS */;
INSERT INTO `item_categories` VALUES (1,'Sembako (Staples)','Kategori Sembako (Staples)','2026-10-20 16:45:17','2026-10-20 16:45:17'),(2,'Minuman (Beverages)','Kategori Minuman (Beverages)','2026-10-20 16:45:17','2026-10-20 16:45:17'),(3,'Makanan Ringan (Snacks)','Kategori Makanan Ringan (Snacks)','2026-10-20 16:45:17','2026-10-20 16:45:17'),(4,'Kebersihan (Household)','Kategori Kebersihan (Household)','2026-10-20 16:45:17','2026-10-20 16:45:17'),(5,'Pertanian (Farming Supplies)','Kategori Pertanian (Farming Supplies)','2026-10-20 16:45:17','2026-10-20 16:45:17');
/*!40000 ALTER TABLE `item_categories` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `items`
--

DROP TABLE IF EXISTS `items`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `items` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `name` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `sku` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `barcode` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `category_id` bigint unsigned DEFAULT NULL,
  `unit_price` decimal(12,2) NOT NULL,
  `cost_price` decimal(12,2) NOT NULL,
  `unit_of_measure` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'pcs',
  `image_path` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `current_stock` int NOT NULL DEFAULT '0',
  `min_stock_threshold` int NOT NULL DEFAULT '10',
  `expiry_date` date DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL DEFAULT '1',
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `items_sku_unique` (`sku`),
  UNIQUE KEY `items_barcode_unique` (`barcode`),
  KEY `items_current_stock_min_stock_threshold_index` (`current_stock`,`min_stock_threshold`),
  KEY `items_active_name_idx` (`is_active`,`name`),
  KEY `items_category_active_idx` (`category_id`,`is_active`),
  CONSTRAINT `items_category_id_foreign` FOREIGN KEY (`category_id`) REFERENCES `item_categories` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=15 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `items`
--

LOCK TABLES `items` WRITE;
/*!40000 ALTER TABLE `items` DISABLE KEYS */;
INSERT INTO `items` VALUES (1,'Beras Premium 5kg','SKU-0001','8991001000015',1,68000.00,60000.00,'sack',NULL,40,10,NULL,1,'2026-10-20 16:45:17','2026-10-20 16:45:17'),(2,'Minyak Goreng 1L','SKU-0002','8991001000022',1,18000.00,15500.00,'bottle',NULL,60,15,NULL,1,'2026-10-20 16:45:17','2026-10-20 16:45:17'),(3,'Gula Pasir 1kg','SKU-0003','8991001000039',1,15000.00,13000.00,'pack',NULL,50,15,NULL,1,'2026-10-20 16:45:17','2026-10-20 16:45:17'),(4,'Teh Celup Kotak','SKU-0004','8991001000046',2,9000.00,7000.00,'box',NULL,35,10,NULL,1,'2026-10-20 16:45:17','2026-10-20 16:45:17'),(5,'Kopi Sachet Renceng','SKU-0005','8991001000053',2,12000.00,9500.00,'pack',NULL,45,10,NULL,1,'2026-10-20 16:45:17','2026-10-20 16:45:17'),(6,'Kerupuk Udang','SKU-0006','8991001000060',3,8000.00,6000.00,'pack',NULL,30,8,NULL,1,'2026-10-20 16:45:17','2026-10-20 16:45:17'),(7,'Biskuit Kaleng','SKU-0007','8991001000077',3,25000.00,20000.00,'tin',NULL,20,5,NULL,1,'2026-10-20 16:45:17','2026-10-20 16:45:17'),(8,'Sabun Cuci Piring','SKU-0008','8991001000084',4,7500.00,6000.00,'bottle',NULL,5,10,NULL,1,'2026-10-20 16:45:17','2026-10-20 16:45:17'),(9,'Deterjen Bubuk 1kg','SKU-0009','8991001000091',4,22000.00,18000.00,'pack',NULL,3,10,NULL,1,'2026-10-20 16:45:17','2026-10-20 16:45:17'),(10,'Pupuk Urea 5kg','SKU-0010','8991001000107',5,45000.00,38000.00,'sack',NULL,25,5,NULL,1,'2026-10-20 16:45:17','2026-10-20 16:45:17');
/*!40000 ALTER TABLE `items` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `jobs`
--

DROP TABLE IF EXISTS `jobs`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `jobs` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `queue` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `payload` longtext COLLATE utf8mb4_unicode_ci NOT NULL,
  `attempts` tinyint unsigned NOT NULL,
  `reserved_at` int unsigned DEFAULT NULL,
  `available_at` int unsigned NOT NULL,
  `created_at` int unsigned NOT NULL,
  PRIMARY KEY (`id`),
  KEY `jobs_queue_index` (`queue`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `jobs`
--

LOCK TABLES `jobs` WRITE;
/*!40000 ALTER TABLE `jobs` DISABLE KEYS */;
/*!40000 ALTER TABLE `jobs` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `loan_payments`
--

DROP TABLE IF EXISTS `loan_payments`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `loan_payments` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `loan_id` bigint unsigned NOT NULL,
  `recorded_by` bigint unsigned DEFAULT NULL,
  `amount` decimal(12,2) NOT NULL,
  `payment_method` enum('cash','qris','bank_transfer') COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'cash',
  `paid_at` timestamp NULL DEFAULT NULL,
  `notes` text COLLATE utf8mb4_unicode_ci,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `loan_payments_recorded_by_foreign` (`recorded_by`),
  KEY `loan_payments_loan_id_paid_at_index` (`loan_id`,`paid_at`),
  CONSTRAINT `loan_payments_loan_id_foreign` FOREIGN KEY (`loan_id`) REFERENCES `loans` (`id`) ON DELETE CASCADE,
  CONSTRAINT `loan_payments_recorded_by_foreign` FOREIGN KEY (`recorded_by`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `loan_payments`
--

LOCK TABLES `loan_payments` WRITE;
/*!40000 ALTER TABLE `loan_payments` DISABLE KEYS */;
INSERT INTO `loan_payments` VALUES (1,1,2,500000.00,'cash','2026-08-20 16:45:18',NULL,'2026-10-20 16:45:18','2026-10-20 16:45:18'),(2,1,NULL,300000.00,'qris','2026-09-20 16:45:18',NULL,'2026-10-20 16:45:18','2026-10-20 16:45:18');
/*!40000 ALTER TABLE `loan_payments` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `loan_products`
--

DROP TABLE IF EXISTS `loan_products`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `loan_products` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `name` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `description` text COLLATE utf8mb4_unicode_ci,
  `interest_rate` decimal(5,4) NOT NULL DEFAULT '0.0000',
  `interest_period` enum('monthly','annual') COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'monthly',
  `min_term_months` int NOT NULL DEFAULT '1',
  `max_term_months` int NOT NULL DEFAULT '12',
  `min_amount` decimal(12,2) NOT NULL DEFAULT '0.00',
  `max_amount` decimal(12,2) DEFAULT NULL,
  `processing_fee_rate` decimal(5,4) NOT NULL DEFAULT '0.0000',
  `eligibility_rule` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL DEFAULT '1',
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `loan_products_is_active_index` (`is_active`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `loan_products`
--

LOCK TABLES `loan_products` WRITE;
/*!40000 ALTER TABLE `loan_products` DISABLE KEYS */;
INSERT INTO `loan_products` VALUES (1,'Pinjaman Darurat','Emergency loan — small, short term, 1% per month.',0.0100,'monthly',1,6,100000.00,5000000.00,0.0100,'Active member for at least 3 months',1,'2026-10-20 16:45:18','2026-10-20 16:45:18'),(2,'Pinjaman Berjangka','Term loan — medium term, 1.5% per month.',0.0150,'monthly',3,12,500000.00,50000000.00,0.0100,'Active member for at least 6 months',1,'2026-10-20 16:45:18','2026-10-20 16:45:18');
/*!40000 ALTER TABLE `loan_products` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `loan_settings`
--

DROP TABLE IF EXISTS `loan_settings`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `loan_settings` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `key` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `value` text COLLATE utf8mb4_unicode_ci,
  `group` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `loan_settings_key_unique` (`key`),
  KEY `loan_settings_group_index` (`group`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `loan_settings`
--

LOCK TABLES `loan_settings` WRITE;
/*!40000 ALTER TABLE `loan_settings` DISABLE KEYS */;
INSERT INTO `loan_settings` VALUES (1,'loan_enabled','true','loan_rules','2026-10-20 16:45:18','2026-10-20 16:45:18'),(2,'max_active_loans_per_member','3','loan_rules','2026-10-20 16:45:18','2026-10-20 16:45:18');
/*!40000 ALTER TABLE `loan_settings` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `loans`
--

DROP TABLE IF EXISTS `loans`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `loans` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `loan_code` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `member_id` bigint unsigned NOT NULL,
  `loan_product_id` bigint unsigned NOT NULL,
  `approved_by` bigint unsigned DEFAULT NULL,
  `principal` decimal(12,2) NOT NULL,
  `interest_rate` decimal(5,4) NOT NULL,
  `interest_period` enum('monthly','annual') COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'monthly',
  `term_months` int NOT NULL,
  `processing_fee` decimal(12,2) NOT NULL DEFAULT '0.00',
  `outstanding_balance` decimal(14,2) NOT NULL,
  `accrued_interest` decimal(12,2) NOT NULL DEFAULT '0.00',
  `total_paid` decimal(12,2) NOT NULL DEFAULT '0.00',
  `status` enum('draft','pending','approved','rejected','active','paid_off','written_off') COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'pending',
  `applied_at` timestamp NULL DEFAULT NULL,
  `approved_at` timestamp NULL DEFAULT NULL,
  `first_due_at` timestamp NULL DEFAULT NULL,
  `due_at` timestamp NULL DEFAULT NULL,
  `paid_off_at` timestamp NULL DEFAULT NULL,
  `notes` text COLLATE utf8mb4_unicode_ci,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `loans_loan_code_unique` (`loan_code`),
  KEY `loans_loan_product_id_foreign` (`loan_product_id`),
  KEY `loans_approved_by_foreign` (`approved_by`),
  KEY `loans_member_id_status_index` (`member_id`,`status`),
  KEY `loans_status_due_at_index` (`status`,`due_at`),
  KEY `loans_approved_at_index` (`approved_at`),
  CONSTRAINT `loans_approved_by_foreign` FOREIGN KEY (`approved_by`) REFERENCES `users` (`id`) ON DELETE SET NULL,
  CONSTRAINT `loans_loan_product_id_foreign` FOREIGN KEY (`loan_product_id`) REFERENCES `loan_products` (`id`) ON DELETE RESTRICT,
  CONSTRAINT `loans_member_id_foreign` FOREIGN KEY (`member_id`) REFERENCES `members` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `loans`
--

LOCK TABLES `loans` WRITE;
/*!40000 ALTER TABLE `loans` DISABLE KEYS */;
INSERT INTO `loans` VALUES (1,'LN-26-DEMO01',1,1,2,2000000.00,0.0100,'monthly',6,20000.00,1250000.00,15000.00,800000.00,'active','2026-06-20 16:45:18','2026-06-20 16:45:18','2026-07-20 16:45:18','2026-12-20 16:45:18',NULL,NULL,'2026-10-20 16:45:18','2026-10-20 16:45:18'),(2,'LN-26-DEMO02',1,2,NULL,5000000.00,0.0150,'monthly',12,50000.00,5050000.00,0.00,0.00,'pending','2026-10-17 16:45:18',NULL,NULL,NULL,NULL,'Application submitted via member portal.','2026-10-20 16:45:18','2026-10-20 16:45:18');
/*!40000 ALTER TABLE `loans` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `members`
--

DROP TABLE IF EXISTS `members`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `members` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `membership_id` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `name` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `email` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `password` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `phone` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `address` text COLLATE utf8mb4_unicode_ci,
  `avatar_path` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `locale` varchar(5) COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'en',
  `join_date` date NOT NULL,
  `shu_balance` decimal(12,2) NOT NULL DEFAULT '0.00',
  `is_active` tinyint(1) NOT NULL DEFAULT '1',
  `remember_token` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `members_membership_id_unique` (`membership_id`),
  UNIQUE KEY `members_email_unique` (`email`),
  UNIQUE KEY `members_phone_unique` (`phone`),
  KEY `members_active_name_idx` (`is_active`,`name`)
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `members`
--

LOCK TABLES `members` WRITE;
/*!40000 ALTER TABLE `members` DISABLE KEYS */;
INSERT INTO `members` VALUES (1,'KOP-26-PQOQN','Sri Wahyuni','sri@example.test','$2y$12$P3PovkLzXbAJ0inzoSBfgePjN217FKpDayOO7nCt01x6W6ib0cihu','081399990001','Desa Sukamaju, RT 01',NULL,'en','2026-04-20',4950.00,1,NULL,'2026-10-20 16:45:17','2026-10-20 16:45:18'),(2,'KOP-26-VSNXO','Joko Prasetyo','joko@example.test','$2y$12$2IXBj51TrC9F.y4GnWg2h.Kdh7mgqIGC6j4riLDxi5hXMeCTv182a','081399990002','Desa Sukamaju, RT 02',NULL,'en','2026-03-20',3460.00,1,NULL,'2026-10-20 16:45:18','2026-10-20 16:45:18'),(3,'KOP-26-DCNEG','Rina Marlina','rina@example.test','$2y$12$6NS6HNpviqcv82daKvtUF.lKfFDnHzSAD1SoYi5wTQRP5YZdWC2pG','081399990003','Desa Sukamaju, RT 03',NULL,'en','2026-02-20',2130.00,1,NULL,'2026-10-20 16:45:18','2026-10-20 16:45:18');
/*!40000 ALTER TABLE `members` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `migrations`
--

DROP TABLE IF EXISTS `migrations`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `migrations` (
  `id` int unsigned NOT NULL AUTO_INCREMENT,
  `migration` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `batch` int NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=19 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `migrations`
--

LOCK TABLES `migrations` WRITE;
/*!40000 ALTER TABLE `migrations` DISABLE KEYS */;
INSERT INTO `migrations` VALUES (1,'0001_01_01_000000_create_users_table',1),(2,'0001_01_01_000001_create_cache_table',1),(3,'0001_01_01_000002_create_jobs_table',1),(4,'2024_01_01_000010_create_members_table',1),(5,'2024_01_01_000020_create_item_categories_table',1),(6,'2024_01_01_000030_create_suppliers_table',1),(7,'2024_01_01_000040_create_items_table',1),(8,'2024_01_01_000041_create_loan_tables',1),(9,'2024_01_01_000050_create_restocking_records_table',1),(10,'2024_01_01_000060_create_discounts_table',1),(11,'2024_01_01_000070_create_transactions_table',1),(12,'2024_01_01_000080_create_transaction_items_table',1),(13,'2024_01_01_000090_create_audit_logs_table',1),(14,'2024_01_01_000100_create_settings_table',1),(15,'2024_01_01_000110_create_personal_access_tokens_table',1),(16,'2024_01_01_000120_add_avatar_to_users_and_members',1),(17,'2024_01_01_000130_add_locale_to_users_and_members',1),(18,'2026_09_22_000001_add_read_performance_indexes',1);
/*!40000 ALTER TABLE `migrations` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `password_reset_tokens`
--

DROP TABLE IF EXISTS `password_reset_tokens`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `password_reset_tokens` (
  `email` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `token` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  PRIMARY KEY (`email`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `password_reset_tokens`
--

LOCK TABLES `password_reset_tokens` WRITE;
/*!40000 ALTER TABLE `password_reset_tokens` DISABLE KEYS */;
/*!40000 ALTER TABLE `password_reset_tokens` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `personal_access_tokens`
--

DROP TABLE IF EXISTS `personal_access_tokens`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `personal_access_tokens` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `tokenable_type` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `tokenable_id` bigint unsigned NOT NULL,
  `name` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `token` varchar(64) COLLATE utf8mb4_unicode_ci NOT NULL,
  `abilities` text COLLATE utf8mb4_unicode_ci,
  `last_used_at` timestamp NULL DEFAULT NULL,
  `expires_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `personal_access_tokens_token_unique` (`token`),
  KEY `personal_access_tokens_tokenable_type_tokenable_id_index` (`tokenable_type`,`tokenable_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `personal_access_tokens`
--

LOCK TABLES `personal_access_tokens` WRITE;
/*!40000 ALTER TABLE `personal_access_tokens` DISABLE KEYS */;
/*!40000 ALTER TABLE `personal_access_tokens` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `restocking_records`
--

DROP TABLE IF EXISTS `restocking_records`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `restocking_records` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `item_id` bigint unsigned NOT NULL,
  `supplier_id` bigint unsigned DEFAULT NULL,
  `quantity` int NOT NULL,
  `cost_per_unit` decimal(12,2) NOT NULL,
  `total_cost` decimal(12,2) NOT NULL,
  `submitted_by` bigint unsigned NOT NULL,
  `notes` text COLLATE utf8mb4_unicode_ci,
  `restocked_at` timestamp NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `restocking_records_supplier_id_foreign` (`supplier_id`),
  KEY `restocks_submitter_restocked_idx` (`submitted_by`,`restocked_at`),
  KEY `restocks_item_restocked_idx` (`item_id`,`restocked_at`),
  KEY `restocks_restocked_idx` (`restocked_at`),
  CONSTRAINT `restocking_records_item_id_foreign` FOREIGN KEY (`item_id`) REFERENCES `items` (`id`) ON DELETE CASCADE,
  CONSTRAINT `restocking_records_submitted_by_foreign` FOREIGN KEY (`submitted_by`) REFERENCES `users` (`id`) ON DELETE RESTRICT,
  CONSTRAINT `restocking_records_supplier_id_foreign` FOREIGN KEY (`supplier_id`) REFERENCES `suppliers` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `restocking_records`
--

LOCK TABLES `restocking_records` WRITE;
/*!40000 ALTER TABLE `restocking_records` DISABLE KEYS */;
INSERT INTO `restocking_records` VALUES (1,1,1,20,60000.00,1200000.00,3,'Initial demo restock','2026-10-10 16:45:17','2026-10-20 16:45:17','2026-10-20 16:45:17'),(2,2,2,20,15500.00,310000.00,3,'Initial demo restock','2026-10-11 16:45:17','2026-10-20 16:45:17','2026-10-20 16:45:17'),(3,3,3,20,13000.00,260000.00,3,'Initial demo restock','2026-10-12 16:45:17','2026-10-20 16:45:17','2026-10-20 16:45:17'),(4,4,1,20,7000.00,140000.00,3,'Initial demo restock','2026-10-13 16:45:17','2026-10-20 16:45:17','2026-10-20 16:45:17'),(5,5,2,20,9500.00,190000.00,3,'Initial demo restock','2026-10-14 16:45:17','2026-10-20 16:45:17','2026-10-20 16:45:17');
/*!40000 ALTER TABLE `restocking_records` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `sessions`
--

DROP TABLE IF EXISTS `sessions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `sessions` (
  `id` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `user_id` bigint unsigned DEFAULT NULL,
  `ip_address` varchar(45) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `user_agent` text COLLATE utf8mb4_unicode_ci,
  `payload` longtext COLLATE utf8mb4_unicode_ci NOT NULL,
  `last_activity` int NOT NULL,
  PRIMARY KEY (`id`),
  KEY `sessions_user_id_index` (`user_id`),
  KEY `sessions_last_activity_index` (`last_activity`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `sessions`
--

LOCK TABLES `sessions` WRITE;
/*!40000 ALTER TABLE `sessions` DISABLE KEYS */;
/*!40000 ALTER TABLE `sessions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `settings`
--

DROP TABLE IF EXISTS `settings`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `settings` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `key` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `value` text COLLATE utf8mb4_unicode_ci,
  `group` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'general',
  `is_public` tinyint(1) NOT NULL DEFAULT '0',
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `settings_key_unique` (`key`)
) ENGINE=InnoDB AUTO_INCREMENT=10 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `settings`
--

LOCK TABLES `settings` WRITE;
/*!40000 ALTER TABLE `settings` DISABLE KEYS */;
INSERT INTO `settings` VALUES (1,'store_name','Koperasi Sejahtera Bersama','infrastructure',1,'2026-10-20 16:45:16','2026-10-20 16:45:16'),(2,'store_address','Jl. Raya Desa No. 1, Malang, Jawa Timur','infrastructure',1,'2026-10-20 16:45:16','2026-10-20 16:45:16'),(3,'api_base_url','http://localhost','infrastructure',0,'2026-10-20 16:45:16','2026-10-20 16:45:16'),(4,'db_host','mysql','infrastructure',0,'2026-10-20 16:45:16','2026-10-20 16:45:16'),(5,'db_port','3306','infrastructure',0,'2026-10-20 16:45:16','2026-10-20 16:45:16'),(6,'tax_rate','0','business_rules',0,'2026-10-20 16:45:16','2026-10-20 16:45:16'),(7,'shu_rate','0.02','business_rules',0,'2026-10-20 16:45:16','2026-10-20 16:45:16'),(8,'low_stock_default_threshold','10','business_rules',0,'2026-10-20 16:45:16','2026-10-20 16:45:16'),(9,'backup_schedule_cron','0 2 * * *','backup',0,'2026-10-20 16:45:16','2026-10-20 16:45:16');
/*!40000 ALTER TABLE `settings` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `suppliers`
--

DROP TABLE IF EXISTS `suppliers`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `suppliers` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `name` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `contact_person` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `phone` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `email` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `address` text COLLATE utf8mb4_unicode_ci,
  `notes` text COLLATE utf8mb4_unicode_ci,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `suppliers`
--

LOCK TABLES `suppliers` WRITE;
/*!40000 ALTER TABLE `suppliers` DISABLE KEYS */;
INSERT INTO `suppliers` VALUES (1,'CV Sumber Makmur','Pak Hendra','081311110001',NULL,NULL,NULL,'2026-10-20 16:45:17','2026-10-20 16:45:17'),(2,'PT Distribusi Nusantara','Bu Rina','081311110002',NULL,NULL,NULL,'2026-10-20 16:45:17','2026-10-20 16:45:17'),(3,'Koperasi Tani Makmur','Pak Slamet','081311110003',NULL,NULL,NULL,'2026-10-20 16:45:17','2026-10-20 16:45:17');
/*!40000 ALTER TABLE `suppliers` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `transaction_items`
--

DROP TABLE IF EXISTS `transaction_items`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `transaction_items` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `transaction_id` bigint unsigned NOT NULL,
  `item_id` bigint unsigned NOT NULL,
  `quantity` int NOT NULL,
  `unit_price` decimal(12,2) NOT NULL,
  `subtotal` decimal(12,2) NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `transaction_items_transaction_id_foreign` (`transaction_id`),
  KEY `trx_items_item_trx_idx` (`item_id`,`transaction_id`),
  CONSTRAINT `transaction_items_item_id_foreign` FOREIGN KEY (`item_id`) REFERENCES `items` (`id`) ON DELETE RESTRICT,
  CONSTRAINT `transaction_items_transaction_id_foreign` FOREIGN KEY (`transaction_id`) REFERENCES `transactions` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=25 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `transaction_items`
--

LOCK TABLES `transaction_items` WRITE;
/*!40000 ALTER TABLE `transaction_items` DISABLE KEYS */;
INSERT INTO `transaction_items` VALUES (1,1,10,2,45000.00,90000.00,'2026-10-20 16:45:18','2026-10-20 16:45:18'),(2,2,4,1,9000.00,9000.00,'2026-10-20 16:45:18','2026-10-20 16:45:18'),(3,2,7,2,25000.00,50000.00,'2026-10-20 16:45:18','2026-10-20 16:45:18'),(4,2,9,3,22000.00,66000.00,'2026-10-20 16:45:18','2026-10-20 16:45:18'),(5,3,2,3,18000.00,54000.00,'2026-10-20 16:45:18','2026-10-20 16:45:18'),(6,3,9,2,22000.00,44000.00,'2026-10-20 16:45:18','2026-10-20 16:45:18'),(7,4,1,3,68000.00,204000.00,'2026-10-20 16:45:18','2026-10-20 16:45:18'),(8,4,5,3,12000.00,36000.00,'2026-10-20 16:45:18','2026-10-20 16:45:18'),(9,4,8,1,7500.00,7500.00,'2026-10-20 16:45:18','2026-10-20 16:45:18'),(10,5,1,3,68000.00,204000.00,'2026-10-20 16:45:18','2026-10-20 16:45:18'),(11,5,10,2,45000.00,90000.00,'2026-10-20 16:45:18','2026-10-20 16:45:18'),(12,6,3,2,15000.00,30000.00,'2026-10-20 16:45:18','2026-10-20 16:45:18'),(13,6,4,2,9000.00,18000.00,'2026-10-20 16:45:18','2026-10-20 16:45:18'),(14,7,3,3,15000.00,45000.00,'2026-10-20 16:45:18','2026-10-20 16:45:18'),(15,7,8,2,7500.00,15000.00,'2026-10-20 16:45:18','2026-10-20 16:45:18'),(16,7,9,3,22000.00,66000.00,'2026-10-20 16:45:18','2026-10-20 16:45:18'),(17,8,4,1,9000.00,9000.00,'2026-10-20 16:45:18','2026-10-20 16:45:18'),(18,8,8,1,7500.00,7500.00,'2026-10-20 16:45:18','2026-10-20 16:45:18'),(19,8,10,2,45000.00,90000.00,'2026-10-20 16:45:18','2026-10-20 16:45:18');
/*!40000 ALTER TABLE `transaction_items` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `transactions`
--

DROP TABLE IF EXISTS `transactions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `transactions` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `transaction_code` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `member_id` bigint unsigned DEFAULT NULL,
  `cashier_id` bigint unsigned NOT NULL,
  `subtotal` decimal(12,2) NOT NULL,
  `discount_id` bigint unsigned DEFAULT NULL,
  `discount_amount` decimal(12,2) NOT NULL DEFAULT '0.00',
  `tax_amount` decimal(12,2) NOT NULL DEFAULT '0.00',
  `total` decimal(12,2) NOT NULL,
  `payment_method` enum('cash','qris','bank_transfer') COLLATE utf8mb4_unicode_ci NOT NULL,
  `payment_status` enum('paid','pending','void') COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'paid',
  `paid_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `transactions_transaction_code_unique` (`transaction_code`),
  KEY `transactions_discount_id_foreign` (`discount_id`),
  KEY `transactions_created_at_index` (`created_at`),
  KEY `transactions_payment_method_payment_status_index` (`payment_method`,`payment_status`),
  KEY `transactions_cashier_created_idx` (`cashier_id`,`created_at`),
  KEY `transactions_member_created_idx` (`member_id`,`created_at`),
  KEY `transactions_created_status_idx` (`created_at`,`payment_status`),
  CONSTRAINT `transactions_cashier_id_foreign` FOREIGN KEY (`cashier_id`) REFERENCES `users` (`id`) ON DELETE RESTRICT,
  CONSTRAINT `transactions_discount_id_foreign` FOREIGN KEY (`discount_id`) REFERENCES `discounts` (`id`) ON DELETE SET NULL,
  CONSTRAINT `transactions_member_id_foreign` FOREIGN KEY (`member_id`) REFERENCES `members` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=14 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `transactions`
--

LOCK TABLES `transactions` WRITE;
/*!40000 ALTER TABLE `transactions` DISABLE KEYS */;
INSERT INTO `transactions` VALUES (1,'TRX-DEMO-VD5H6J',NULL,3,90000.00,NULL,0.00,0.00,90000.00,'cash','paid','2026-10-13 16:45:18','2026-10-20 16:45:18','2026-10-20 16:45:18'),(2,'TRX-DEMO-NZBS7Q',2,3,125000.00,NULL,0.00,0.00,125000.00,'cash','paid','2026-10-11 16:45:18','2026-10-20 16:45:18','2026-10-20 16:45:18'),(3,'TRX-DEMO-ZONAEX',NULL,3,98000.00,NULL,0.00,0.00,98000.00,'qris','paid','2026-10-08 16:45:18','2026-10-20 16:45:18','2026-10-20 16:45:18'),(4,'TRX-DEMO-HSNIM4',1,3,247500.00,NULL,0.00,0.00,247500.00,'cash','paid','2026-10-14 16:45:18','2026-10-20 16:45:18','2026-10-20 16:45:18'),(5,'TRX-DEMO-OV1QCS',NULL,3,294000.00,NULL,0.00,0.00,294000.00,'qris','paid','2026-10-14 16:45:18','2026-10-20 16:45:18','2026-10-20 16:45:18'),(6,'TRX-DEMO-0BPLH9',2,3,48000.00,NULL,0.00,0.00,48000.00,'cash','paid','2026-10-10 16:45:18','2026-10-20 16:45:18','2026-10-20 16:45:18'),(7,'TRX-DEMO-NBHGDT',NULL,3,126000.00,NULL,0.00,0.00,126000.00,'qris','paid','2026-10-11 16:45:18','2026-10-20 16:45:18','2026-10-20 16:45:18'),(8,'TRX-DEMO-SL1AQ9',3,3,106500.00,NULL,0.00,0.00,106500.00,'bank_transfer','paid','2026-10-16 16:45:18','2026-10-20 16:45:18','2026-10-20 16:45:18');
/*!40000 ALTER TABLE `transactions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `users`
--

DROP TABLE IF EXISTS `users`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `users` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `name` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `email` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `email_verified_at` timestamp NULL DEFAULT NULL,
  `password` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `role` enum('admin','shop_owner','employee') COLLATE utf8mb4_unicode_ci NOT NULL,
  `phone` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `shift_label` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `avatar_path` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `locale` varchar(5) COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'en',
  `is_active` tinyint(1) NOT NULL DEFAULT '1',
  `remember_token` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `users_email_unique` (`email`),
  KEY `users_role_index` (`role`),
  KEY `users_active_name_idx` (`is_active`,`name`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `users`
--

LOCK TABLES `users` WRITE;
/*!40000 ALTER TABLE `users` DISABLE KEYS */;
INSERT INTO `users` VALUES (1,'Budi Santoso','admin@koperasi.test',NULL,'$2y$12$KxNPJOi3foCpxRoEiMQTyO57anEo5/e.LWYlHeu9hzidq.mu8nY5C','admin','081200000001',NULL,NULL,'en',1,NULL,'2026-10-20 16:45:17','2026-10-20 16:45:17'),(2,'Siti Aminah','owner@koperasi.test',NULL,'$2y$12$mDqnXtOLqglFeA10cXaQiuFZBOFf4j1Mt8qiepjqQi/ibXrjRF72y','shop_owner','081200000002',NULL,NULL,'en',1,NULL,'2026-10-20 16:45:17','2026-10-20 16:45:17'),(3,'Agus Wijaya','kasir1@koperasi.test',NULL,'$2y$12$nEjqkZ63OnPd09yZYVGrXeOV6TkW8tRHNhHXgcRRPaeg.6BvgKb7q','employee','081200000003','Pagi (07:00-15:00)',NULL,'en',1,NULL,'2026-10-20 16:45:17','2026-10-20 16:45:17'),(4,'Dewi Lestari','kasir2@koperasi.test',NULL,'$2y$12$g2HyIpFC9sJwvnHsuLqq1uR0ER0hXYcuMERJII8oFPJ6qh4XZ1n8e','employee','081200000004','Sore (15:00-21:00)',NULL,'en',1,NULL,'2026-10-20 16:45:17','2026-10-20 16:45:17');
/*!40000 ALTER TABLE `users` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-10-20  9:56:53
