# Script de testare - MediClin

## 1. Date generale

Aplicatia testata: MediClin  
Tip aplicatie: Desktop Windows / WPF  
Data testarii: 19.05.2026  
Metode utilizate: testare manuala si testare automata cu Katalon Studio  
Executabil testat: `D:\medclin\Mediclin\Mediclin.UI\bin\Debug\net8.0-windows\Mediclin.UI.exe`

Conturi utilizate la testare:

| Rol | Email | Parola |
| --- | --- | --- |
| Pacient | `<email_pacient>` | `<parola_pacient>` |
| Medic | `<email_medic>` | `<parola_medic>` |
| Administrator | `<email_admin>` | `<parola_admin>` |

## 2. Script pentru testarea manuala

### TC-01 - Autentificare pacient

Scop: verificarea accesului pacientului in aplicatie.

Pasi:

1. Se porneste aplicatia MediClin.
2. Se introduce emailul pacientului.
3. Se introduce parola pacientului.
4. Se apasa butonul `Autentificati-va`.

Date introduse:

- Email: `<email_pacient>`
- Parola: `<parola_pacient>`

Rezultat asteptat: se deschide dashboard-ul pacientului si se afiseaza numele pacientului.

Rezultat obtinut: dashboard-ul pacientului este afisat corect.

Concluzie: test admis.

### TC-02 - Autentificare cu date gresite

Scop: verificarea validarii datelor de login.

Pasi:

1. Se porneste aplicatia MediClin.
2. Se introduce un email gresit.
3. Se introduce o parola gresita.
4. Se apasa butonul `Autentificati-va`.

Date introduse:

- Email: `gresit@test.com`
- Parola: `12345`

Rezultat asteptat: aplicatia nu permite autentificarea si afiseaza mesaj de eroare.

Rezultat obtinut: autentificarea este refuzata.

Concluzie: test admis.

### TC-03 - Navigare in meniul pacientului

Scop: verificarea tuturor butoanelor din meniul pacientului.

Pasi:

1. Se efectueaza login ca pacient.
2. Se apasa pe `Dashboard pacient`.
3. Se apasa pe `Programarile mele`.
4. Se apasa pe `Fisa medicala`.
5. Se apasa pe `Retete active`.
6. Se apasa pe `Rezultate analize`.
7. Se apasa pe `Mesaje medic`.
8. Se apasa pe `Setari cont`.

Date introduse: nu sunt necesare date suplimentare.

Rezultat asteptat: fiecare pagina se deschide fara erori.

Rezultat obtinut: paginile sunt accesibile din meniu.

Concluzie: test admis.

### TC-04 - Selectare consultatie din istoric

Scop: verificarea completarii automate a fisei medicale din istoricul consultatiilor.

Pasi:

1. Se efectueaza login ca pacient.
2. Se acceseaza pagina `Fisa medicala`.
3. Se localizeaza zona `Consultation History`.
4. Se selecteaza o consultatie existenta.

Date introduse: click pe o consultatie din istoric.

Rezultat asteptat: campurile pentru simptome, diagnostic, semne vitale si recomandari se completeaza automat.

Rezultat obtinut: informatiile consultatiei selectate apar in boxurile fisei.

Concluzie: test admis.

### TC-05 - Solicitare reinnoire reteta

Scop: verificarea fluxului prin care pacientul cere reinnoirea unei retete.

Pasi:

1. Se efectueaza login ca pacient.
2. Se acceseaza pagina `Retete active`.
3. Se selecteaza o reteta existenta.
4. Se apasa butonul `Solicita reinnoire`.

Date introduse: reteta selectata din lista.

Rezultat asteptat: cererea este transmisa medicului de familie.

Rezultat obtinut: solicitarea apare la medic pentru reteta aleasa.

Concluzie: test admis.

### TC-06 - Autentificare medic

Scop: verificarea accesului medicului in workspace.

Pasi:

1. Se porneste aplicatia MediClin.
2. Se introduce emailul medicului.
3. Se introduce parola medicului.
4. Se apasa butonul `Autentificati-va`.

Date introduse:

- Email: `<email_medic>`
- Parola: `<parola_medic>`

Rezultat asteptat: se deschide workspace-ul medicului.

Rezultat obtinut: workspace-ul medicului este afisat.

Concluzie: test admis.

### TC-07 - Cautare pacient

Scop: verificarea cautarii pacientilor de catre medic.

Pasi:

1. Se efectueaza login ca medic.
2. Se acceseaza pagina `Pacienti`.
3. Se introduce numele unui pacient in campul de cautare.

Date introduse: `Marius`.

Rezultat asteptat: lista este filtrata si afiseaza pacientul cautat.

Rezultat obtinut: pacientul cautat ramane vizibil in lista.

Concluzie: test admis.

### TC-08 - Adaugare alergie

Scop: verificarea posibilitatii de a salva alergii pentru pacient.

Pasi:

1. Se efectueaza login ca medic.
2. Se acceseaza pagina `Pacienti`.
3. Se deschide fisa pacientului.
4. Se completeaza campul pentru alergii.
5. Se salveaza informatia.

Date introduse: `Penicilina`.

Rezultat asteptat: alergia introdusa apare in lista de alergii active.

Rezultat obtinut: alergia este salvata si afisata.

Concluzie: test admis.

### TC-09 - Transmitere rezultate analize

Scop: verificarea trimiterii rezultatelor de analize de la medic catre pacient.

Pasi:

1. Se efectueaza login ca medic.
2. Se acceseaza pagina pacientului.
3. Se deschide sectiunea de analize.
4. Se completeaza rezultatul analizei.
5. Se apasa butonul de trimitere/salvare.
6. Se efectueaza login ca pacient.
7. Se acceseaza pagina `Rezultate analize`.

Date introduse:

- Laborator: `MediLab`
- Test: `Hemoglobina`
- Valoare: `14 g/dL`
- Status: `Normal`

Rezultat asteptat: rezultatul introdus de medic apare in contul pacientului.

Rezultat obtinut: rezultatul este vizibil in pagina pacientului.

Concluzie: test admis.

### TC-10 - Pagina financiara

Scop: verificarea afisarii informatiilor financiare.

Pasi:

1. Se efectueaza login ca medic sau administrator.
2. Se acceseaza pagina `Financiar`.
3. Se verifica valorile afisate pentru venituri, consultatii si estimari.

Date introduse: nu sunt necesare date suplimentare.

Rezultat asteptat: pagina afiseaza informatii financiare centralizate.

Rezultat obtinut: datele financiare sunt vizibile.

Concluzie: test admis.

## 3. Script pentru testarea automata in Katalon Studio

### Pregatire

1. Se deschide Katalon Studio.
2. Se creeaza un proiect nou pentru Desktop/Windows.
3. Se deschide recorder-ul pentru aplicatii Windows.
4. Se selecteaza executabilul:

`D:\medclin\Mediclin\Mediclin.UI\bin\Debug\net8.0-windows\Mediclin.UI.exe`

5. Se inregistreaza pasii principali: login, navigare, selectare fisa, reinnoire reteta, trimitere analiza.
6. Obiectele capturate de recorder se salveaza in `Object Repository`.
7. Se ruleaza test case-ul si se salveaza raportul.

### Scenariu automat recomandat

1. Porneste aplicatia.
2. Completeaza emailul pacientului.
3. Completeaza parola pacientului.
4. Apasa `Autentificati-va`.
5. Verifica aparitia dashboard-ului pacientului.
6. Deschide `Fisa medicala`.
7. Selecteaza o consultatie din istoric.
8. Verifica aparitia informatiilor in campurile fisei.
9. Deschide `Retete active`.
10. Selecteaza o reteta.
11. Apasa `Solicita reinnoire`.
12. Inchide aplicatia.

## 4. Model de concluzie dupa rulare

In urma testarii manuale, functionalitatile principale ale aplicatiei MediClin au fost verificate prin accesarea paginilor si butoanelor importante. Rezultatele obtinute au coincis cu rezultatele asteptate, iar testele au fost marcate ca admise.

In urma testarii automate cu Katalon Studio, pasii inregistrati au fost rulati automat. Raportul generat de Katalon confirma statusul testelor si poate fi atasat in capitolul de testare sub forma de captura de ecran.
