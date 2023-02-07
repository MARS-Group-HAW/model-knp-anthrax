# KNP Anthrax Model

- Average displacement numbers (Amélie email from 2022.11.29):
  - Kudu:
    - Morning displacement = 1.13 km
    - Afternoon displacement = 1.17 km
    - Night displacement = 0.92 km
    - Total: 3.22 km
  - Impala:
    - Morning displacement = 0.78 km
    - Afternoon displacement = 0.82 km
    - Night displacement = 0.65 km
    - Total: 2.35 km

---

Google Doc: <https://docs.google.com/document/d/1KbDwEofh2wF70RYHeVue7AzdKv5L7jh1ZKkbXSnPawg/edit?skip_itp2_check=true&pli=1>

---

Meeting (Dec 20, 2022):

- Sunday: narrow down classification labels
- Amélie: continue working on infection lifecycle and spore dispersion dynamic

---

Meeting (Jan 31, 2023):

- To-do for us:
  - Consider making landscape types weighted (instead of all-or-nothing)
- Landscape type preferences:
  - Kudu prefers woodland
  - Impala prefers woodland and plains
- Sunday/Amélie:
  - DONE Decide which water data to use
  - DONE Finalize landscape type classification
  - DONE Share water sources data (Google Drive)
  - DONE Specify landscape type weighting
  - Specify maxDistance from nearest water source

---

Meeting (Feb 07, 2023):

- To-do:
  - Add land type preference distributions to model
  - Try distributions with 10 kudus and 10 impalas to see if it makes sense
  - Add Amélie to GitHub repository
  - Water sources to include:
    - River area and water holes (Sunday will send SHP)
  - Add configurable water max distance (with INF for now)
  - Infection model:
    - Animals die between 3-14 days
  - Rudimentary reproduction dynamic
- To-do Amélie/Sunday:
  - Define reproduction dynamic
