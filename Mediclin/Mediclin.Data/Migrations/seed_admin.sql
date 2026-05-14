-- Inserir dados iniciais - Admin
-- Nota: A senha é hash bcrypt para "Admin@123"
INSERT INTO utilizatori (email, parola_hash, rol, prenume, nume, telefon, activ) 
VALUES ('admin@mediclin.com', '$2a$12$Vy.mzKOqVFcNV7nQkdp2HeC1d.mVCjh2U2lzLCvTqVeUIGSCFQkVG', 'admin', 'Administrator', 'Mediclin', '0700000000', 1);

-- Inserir dados iniciais - Medic
-- Nota: A senha é hash bcrypt para "Medic@123"
INSERT INTO utilizatori (email, parola_hash, rol, prenume, nume, telefon, activ) 
VALUES ('medic@mediclin.com', '$2a$12$8mxCvLQ8VnL9X5sR8Z3KqOv2kDrJ6pL8Q2wN5qB3mV7gU2sT9eK6m', 'medic', 'Doctor', 'Popescu', '0701234567', 1);

-- Inserir dados iniciais - Pacient
-- Nota: A senha é hash bcrypt para "Pacient@123"
INSERT INTO utilizatori (email, parola_hash, rol, prenume, nume, telefon, activ) 
VALUES ('pacient@mediclin.com', '$2a$12$9qZ7hK2mN1L0oP3sR5tU6VvW4xY9zQ8cDeFgHiJkLmNoPqRsTuVw', 'pacient', 'Ioan', 'Ionescu', '0702345678', 1);

-- Criar perfis para os utilizadores criados (mínimo necessário)
INSERT INTO pacienti (utilizador_id, data_nasterii, gen, numar_cnp) 
VALUES (3, '1990-05-15', 'M', '1900515012345');

-- Inserir especialidades
INSERT INTO specialitati (nume, descriere) VALUES 
('Cardiologie', 'Specialitate în boli de inimă și vasculare'),
('Dermatologie', 'Specialitate în boli de piele'),
('Neurologie', 'Specialitate în boli neurologice'),
('Oftalmologie', 'Specialitate în boli oculare'),
('Pediatrie', 'Specialitate în medicina copilului'),
('Ortopedie', 'Specialitate în boli osoase și articulare');

-- Crear perfil de medic para o medico criado
INSERT INTO medici (utilizator_id, specialitate_id, numar_inmatriculare, pret_consultatie)
VALUES (2, 1, 'MED001', 150.00);

-- Criar programa de medico para teste
INSERT INTO program_medici (medic_id, zi_saptamana, ora_start, ora_sfarsit)
VALUES 
(1, 1, '09:00:00', '17:00:00'),
(1, 2, '09:00:00', '17:00:00'),
(1, 3, '09:00:00', '17:00:00'),
(1, 4, '09:00:00', '17:00:00'),
(1, 5, '09:00:00', '17:00:00');
