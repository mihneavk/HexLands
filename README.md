# 🎲 Catan VS AI - Unity Edition

Un joc complet funcțional bazat pe regulile clasicului board game "Colonistii din Catan", dezvoltat de la zero în Unity (C#). Acest proiect nu este doar o replică a jocului original, ci o demonstrație tehnică avansată de **Inteligență Artificială**, combinând algoritmi de decizie matematici cu un model de limbaj de ultimă generație (LLM).

[Demo](https://youtu.be/9_6zcQmxeuw)

## ✨ Caracteristici Principale

### 🗺️ Hartă Procedurală & Reguli de Bază
* **Generare Dinamică:** Harta este generată procedural la fiecare joc, asigurând o distribuție aleatorie (dar echilibrată) a resurselor și numerelor.
* **Sistem de Construcție:** Jucătorii pot construi Drumuri, Case și Orașe respectând cu strictețe regulile jocului (ex. distanța minimă de 2 drumuri între case).
* **Economie și Trade:** Sistem complet de resurse (Lemn, Argilă, Lână, Grâu, Minereu). Include schimburi comerciale cu banca la rata standard (4:1) sau prin porturi specifice (3:1 sau 2:1).
* **Cărți de Dezvoltare:** Pachet complet de cărți funcționale (Cavaler, Construire Drumuri, Anul Abundenței, Monopol, Punct de Victorie).
* **Regula de 7 & Hoțul:** La aruncarea unui 7, jucătorii cu peste 7 cărți sunt obligați să arunce jumătate, iar Hoțul este mutat pentru a bloca producția unui hexagon și a fura o resursă.
* **Trofee Dinamice:** Calcul automat și transferabil pentru "Cel mai lung drum" și "Cea mai mare armată".

### 🧠 Adversarul AI (Utility AI + Monte Carlo Light)
Inamicul din acest joc nu acționează la întâmplare. A fost dezvoltat un sistem AI complex care analizează și "vede" în viitor:
* **Expected Value (Valoare Așteptată):** AI-ul calculează probabilitatea matematică a zarurilor (ex. știe că un 8 are 5/36 șanse să iasă) și își evaluează producția pe tură.
* **Simulare de Viitor:** Înainte de a face o mutare, AI-ul simulează în memorie rezultatul construirii unei case sau a unui oraș și compară scorul viitor cu scorul obținut dacă ar economisi resursele (Pass).
* **Hoț Malefic și Tactic:** AI-ul alege locul perfect pentru hoț calculând unde va produce cea mai mare "durere" jucătorului uman (vizând numerele roșii), evitând cu strictețe să își blocheze propriile sate.
* **Folosirea Inteligentă a Cărților:** AI-ul își păstrează Cavalerii dacă hoțul nu îl deranjează personal (cu excepția cazului în care luptă pentru trofeul armatei) și alege perfect resursele de care are nevoie când joacă "Anul Abundenței".

### 🤖 LLM Advisor (Integrat cu Microsoft Phi-3)
Jocul include un "Asistent" interactiv alimentat de modelul de limbaj **Phi-3**. 
Acesta primește context din starea jocului și poate oferi jucătorului uman sfaturi strategice, analize ale tablei de joc sau explicații ale regulilor, rulând local (sau prin API) pentru a oferi o experiență de "Mentorship" în timp real.

---

## 🎮 Cum se joacă

La pornirea jocului (`MainMenu`), poți alege dacă vrei să fii Jucătorul 1 (Albastru - începe primul) sau Jucătorul 2 (Portocaliu). Jocul se desfășoară automat:
1. **Faza de Setup:** Se plasează gratuit 2 case și 2 drumuri.
2. **Gameplay:** Dai cu zarul, strângi resurse, faci trade cu banca, construiești și cumperi dezvoltări.
3. **Condiția de Victorie:** Primul care atinge **10 Puncte de Victorie** câștigă jocul, declanșând ecranul de final.

### 🛠️ Controale de Debug / Testare
Pentru a facilita testarea mecanicii, am implementat următoarele comenzi rapide de la tastatură:
* <kbd>R</kbd> - **Resetare Joc:** Reîncarcă instantaneu scena curentă (generează o hartă nouă și resetează punctajele).
* <kbd>\\</kbd> *(Backslash)* - **Cheat Resurse:** Îi acordă jucătorului curent câte **8 bucăți din fiecare resursă** instantaneu. Ideal pentru a testa rapid construcțiile și sistemul de trofee.

---

## 💻 Tehnologii Folosite
* **Motor Grafic:** Unity 3D (C#)
* **Arhitectură:** Orientată pe Obiect (OOP), Meniuri OnGUI pentru UI minimalist.
* **Generativ AI:** Integrare API/Locală cu LLM-ul Phi-3.

## 🚀 Rularea Proiectului
1. Clonează acest repository.
2. Deschide proiectul folosind Unity Editor.
3. În secțiunea *Build Settings*, asigură-te că scena `MainMenu` este pe indexul 0, iar scena Hărții este pe indexul 1.
4. Apasă Play din editor sau dă Build pentru platforma dorită (Windows/Mac/Linux).

<img width="1065" height="1179" alt="uml" src="https://github.com/user-attachments/assets/9bc99c45-37a9-43ce-891e-aaadefefe125" />
<img width="1377" height="1226" alt="image" src="https://github.com/user-attachments/assets/06b289c6-e6c0-4952-a265-4402d08e2a03" />
<img width="1374" height="1242" alt="image" src="https://github.com/user-attachments/assets/cc2ac89a-7f6b-46fa-8532-21047bfe3425" />


