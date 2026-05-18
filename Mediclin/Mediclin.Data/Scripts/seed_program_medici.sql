-- Give all verified doctors a default schedule
-- Mon-Fri 09:00-17:00, 30 min consultations
INSERT IGNORE INTO program_medici
    (medic_id, zi_saptamana, ora_start, ora_sfarsit, activ)
SELECT
    m.id,
    z.zi,
    '09:00:00',
    '17:00:00',
    1
FROM medici m
CROSS JOIN (
    SELECT 'Luni'     AS zi UNION ALL
    SELECT 'Marti'         UNION ALL
    SELECT 'Miercuri'      UNION ALL
    SELECT 'Joi'           UNION ALL
    SELECT 'Vineri'
) z
WHERE m.verificat = 1
ON DUPLICATE KEY UPDATE activ = 1;
