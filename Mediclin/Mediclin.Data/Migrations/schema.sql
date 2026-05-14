-- Criar base de dados
CREATE DATABASE IF NOT EXISTS mediclin_db;
USE mediclin_db;

-- Tabela de utilizadores
CREATE TABLE IF NOT EXISTS utilizatori (
    id INT AUTO_INCREMENT PRIMARY KEY,
    email VARCHAR(255) NOT NULL UNIQUE,
    parola_hash VARCHAR(255) NOT NULL,
    rol VARCHAR(50) NOT NULL DEFAULT 'pacient',
    prenume VARCHAR(100) NOT NULL,
    nume VARCHAR(100) NOT NULL,
    telefon VARCHAR(20),
    avatar_url VARCHAR(500),
    activ TINYINT(1) DEFAULT 1,
    creat_la TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    actualizat_la TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    ultim_login DATETIME,
    INDEX idx_email (email),
    INDEX idx_rol (rol)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Tabela de medici
CREATE TABLE IF NOT EXISTS medici (
    id INT AUTO_INCREMENT PRIMARY KEY,
    utilizator_id INT NOT NULL UNIQUE,
    specialitate_id INT,
    numar_inmatriculare VARCHAR(50) UNIQUE,
    biografie TEXT,
    pret_consultatie DECIMAL(10, 2),
    rata_rating FLOAT DEFAULT 0,
    numar_pacienti INT DEFAULT 0,
    activ TINYINT(1) DEFAULT 1,
    creat_la TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    actualizat_la TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (utilizator_id) REFERENCES utilizatori(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Tabela de pacienti
CREATE TABLE IF NOT EXISTS pacienti (
    id INT AUTO_INCREMENT PRIMARY KEY,
    utilizator_id INT NOT NULL UNIQUE,
    data_nasterii DATE,
    gen VARCHAR(10),
    grupa_sanguina VARCHAR(10),
    numar_cnp VARCHAR(20) UNIQUE,
    adresa VARCHAR(255),
    oras VARCHAR(100),
    judet VARCHAR(100),
    cod_postal VARCHAR(10),
    numar_urgenta VARCHAR(20),
    alergii TEXT,
    boli_cronice TEXT,
    medicament_actuale TEXT,
    creat_la TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    actualizat_la TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (utilizator_id) REFERENCES utilizatori(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Tabela de specialitati
CREATE TABLE IF NOT EXISTS specialitati (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nume VARCHAR(100) NOT NULL UNIQUE,
    descriere TEXT,
    icon_url VARCHAR(500),
    activa TINYINT(1) DEFAULT 1,
    creat_la TIMESTAMP DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Tabela de programari
CREATE TABLE IF NOT EXISTS programari (
    id INT AUTO_INCREMENT PRIMARY KEY,
    pacient_id INT NOT NULL,
    medic_id INT NOT NULL,
    data_ora DATETIME NOT NULL,
    durata_minute INT DEFAULT 30,
    motiv VARCHAR(500),
    stare VARCHAR(50) DEFAULT 'agendare',
    note_medic TEXT,
    pretul DECIMAL(10, 2),
    creat_la TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    actualizat_la TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (pacient_id) REFERENCES pacienti(id) ON DELETE CASCADE,
    FOREIGN KEY (medic_id) REFERENCES medici(id) ON DELETE CASCADE,
    INDEX idx_medic_data (medic_id, data_ora),
    INDEX idx_pacient (pacient_id),
    INDEX idx_stare (stare)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Tabela de consultatii
CREATE TABLE IF NOT EXISTS consultatii (
    id INT AUTO_INCREMENT PRIMARY KEY,
    programare_id INT NOT NULL UNIQUE,
    diagnostic TEXT,
    recomandari TEXT,
    date_vitale JSON,
    creat_la TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    actualizat_la TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (programare_id) REFERENCES programari(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Tabela de retete
CREATE TABLE IF NOT EXISTS retete (
    id INT AUTO_INCREMENT PRIMARY KEY,
    consultatie_id INT NOT NULL,
    medicament_id INT,
    denumire_medicament VARCHAR(255) NOT NULL,
    concentratie VARCHAR(50),
    forma VARCHAR(50),
    cantitate INT,
    unitate VARCHAR(20),
    frecventa_administrare VARCHAR(100),
    durata_tratament_zile INT,
    instructiuni TEXT,
    creat_la TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    actualizat_la TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (consultatie_id) REFERENCES consultatii(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Tabela de analize
CREATE TABLE IF NOT EXISTS analize (
    id INT AUTO_INCREMENT PRIMARY KEY,
    pacient_id INT NOT NULL,
    medic_solicitator_id INT,
    tip_analiza VARCHAR(100) NOT NULL,
    descriere TEXT,
    data_solicitare DATE,
    data_executie DATE,
    laborator VARCHAR(100),
    stare VARCHAR(50) DEFAULT 'asteptare',
    fisier_rezultat VARCHAR(500),
    creat_la TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    actualizat_la TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (pacient_id) REFERENCES pacienti(id) ON DELETE CASCADE,
    FOREIGN KEY (medic_solicitator_id) REFERENCES medici(id),
    INDEX idx_pacient (pacient_id),
    INDEX idx_stare (stare)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Tabela de valori analize
CREATE TABLE IF NOT EXISTS valori_analize (
    id INT AUTO_INCREMENT PRIMARY KEY,
    analiza_id INT NOT NULL,
    parametru VARCHAR(100) NOT NULL,
    valoare VARCHAR(50),
    unitate VARCHAR(50),
    valoare_normala_min VARCHAR(50),
    valoare_normala_max VARCHAR(50),
    stare_rezultat VARCHAR(20),
    FOREIGN KEY (analiza_id) REFERENCES analize(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Tabela de boli cronice
CREATE TABLE IF NOT EXISTS boli_cronice (
    id INT AUTO_INCREMENT PRIMARY KEY,
    pacient_id INT NOT NULL,
    cod_diagnostic VARCHAR(20),
    denumire VARCHAR(255) NOT NULL,
    descriere TEXT,
    data_diagnostic DATE,
    medic_diagnostic_id INT,
    activa TINYINT(1) DEFAULT 1,
    creat_la TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (pacient_id) REFERENCES pacienti(id) ON DELETE CASCADE,
    FOREIGN KEY (medic_diagnostic_id) REFERENCES medici(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Tabela de alergii
CREATE TABLE IF NOT EXISTS alergii (
    id INT AUTO_INCREMENT PRIMARY KEY,
    pacient_id INT NOT NULL,
    denumire VARCHAR(255) NOT NULL,
    tip_alergie VARCHAR(50),
    severitate VARCHAR(50),
    simptome TEXT,
    creat_la TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (pacient_id) REFERENCES pacienti(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Tabela de notificari
CREATE TABLE IF NOT EXISTS notificari (
    id INT AUTO_INCREMENT PRIMARY KEY,
    utilizator_id INT NOT NULL,
    titlu VARCHAR(255) NOT NULL,
    mesaj TEXT,
    tip VARCHAR(50),
    citita TINYINT(1) DEFAULT 0,
    data_citire DATETIME,
    creat_la TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (utilizator_id) REFERENCES utilizatori(id) ON DELETE CASCADE,
    INDEX idx_utilizator_citita (utilizator_id, citita)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Tabela de mesaje
CREATE TABLE IF NOT EXISTS mesaje (
    id INT AUTO_INCREMENT PRIMARY KEY,
    conversatie_id INT,
    expeditor_id INT NOT NULL,
    destinatar_id INT NOT NULL,
    continut TEXT NOT NULL,
    citit TINYINT(1) DEFAULT 0,
    data_citire DATETIME,
    creat_la TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (expeditor_id) REFERENCES utilizatori(id) ON DELETE CASCADE,
    FOREIGN KEY (destinatar_id) REFERENCES utilizatori(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Tabela de conversatii
CREATE TABLE IF NOT EXISTS conversatii (
    id INT AUTO_INCREMENT PRIMARY KEY,
    utilizator1_id INT NOT NULL,
    utilizator2_id INT NOT NULL,
    creat_la TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    actualizat_la TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (utilizator1_id) REFERENCES utilizatori(id) ON DELETE CASCADE,
    FOREIGN KEY (utilizator2_id) REFERENCES utilizatori(id) ON DELETE CASCADE,
    UNIQUE KEY unique_conversation (utilizator1_id, utilizator2_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Tabela de lista conversatii
CREATE TABLE IF NOT EXISTS conversatii_lista (
    id INT AUTO_INCREMENT PRIMARY KEY,
    utilizator_id INT NOT NULL,
    conversatie_id INT NOT NULL,
    ultim_mesaj_id INT,
    citita TINYINT(1) DEFAULT 0,
    FOREIGN KEY (utilizator_id) REFERENCES utilizatori(id) ON DELETE CASCADE,
    FOREIGN KEY (conversatie_id) REFERENCES conversatii(id) ON DELETE CASCADE,
    UNIQUE KEY unique_user_conversation (utilizator_id, conversatie_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Tabela de jurnal (audit)
CREATE TABLE IF NOT EXISTS jurnal (
    id INT AUTO_INCREMENT PRIMARY KEY,
    utilizator_id INT,
    actiune VARCHAR(255) NOT NULL,
    tabel_afectat VARCHAR(100),
    id_rand_afectat INT,
    date_vechi JSON,
    date_noi JSON,
    ip_address VARCHAR(45),
    user_agent VARCHAR(500),
    creat_la TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (utilizator_id) REFERENCES utilizatori(id) ON DELETE SET NULL,
    INDEX idx_data (creat_la),
    INDEX idx_utilizator (utilizator_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Tabela de setari
CREATE TABLE IF NOT EXISTS setari (
    id INT AUTO_INCREMENT PRIMARY KEY,
    utilizator_id INT,
    cheie VARCHAR(255) NOT NULL,
    valoare TEXT,
    tip VARCHAR(50),
    creat_la TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    actualizat_la TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (utilizator_id) REFERENCES utilizatori(id) ON DELETE CASCADE,
    UNIQUE KEY unique_user_setting (utilizator_id, cheie)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Tabela de program medici (orare)
CREATE TABLE IF NOT EXISTS program_medici (
    id INT AUTO_INCREMENT PRIMARY KEY,
    medic_id INT NOT NULL,
    zi_saptamana INT NOT NULL,
    ora_start TIME NOT NULL,
    ora_sfarsit TIME NOT NULL,
    activ TINYINT(1) DEFAULT 1,
    FOREIGN KEY (medic_id) REFERENCES medici(id) ON DELETE CASCADE,
    UNIQUE KEY unique_medic_day (medic_id, zi_saptamana)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Tabela de medicament
CREATE TABLE IF NOT EXISTS medicament (
    id INT AUTO_INCREMENT PRIMARY KEY,
    denumire VARCHAR(255) NOT NULL UNIQUE,
    concentratie VARCHAR(50),
    forma VARCHAR(50),
    producator VARCHAR(100),
    cod_farmacopee VARCHAR(50),
    activ TINYINT(1) DEFAULT 1,
    creat_la TIMESTAMP DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Tabela de rezultat analiza
CREATE TABLE IF NOT EXISTS rezultat_analiza (
    id INT AUTO_INCREMENT PRIMARY KEY,
    analiza_id INT NOT NULL,
    data_rezultat DATETIME,
    fichier_path VARCHAR(500),
    status_validare VARCHAR(50),
    observatii TEXT,
    FOREIGN KEY (analiza_id) REFERENCES analize(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
