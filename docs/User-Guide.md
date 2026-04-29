# 📖 AI Asistan — Kullanım Kılavuzu

## 📋 İçindekiler

- [Giriş](#giriş)
- [1. Giriş Yapma ve Kayıt](#1-giriş-yapma-ve-kayıt)
- [2. Ana Ekran (Chat)](#2-ana-ekran-chat)
- [3. Sohbet İşlemleri](#3-sohbet-i̇şlemleri)
- [4. Döküman Yönetimi](#4-döküman-yönetimi)
- [5. Rapor İşlemleri](#5-rapor-i̇şlemleri)
- [6. Filtreleme Sistemi](#6-filtreleme-sistemi)
- [7. Geri Bildirim (Feedback)](#7-geri-bildirim-feedback)
- [8. Dashboard (Analitik)](#8-dashboard-analitik)
- [9. Hazır Raporlar (AdventureWorks)](#9-hazır-raporlar-adventureworks)
- [10. Klavye Kısayolları](#10-klavye-kısayolları)
- [11. Sık Sorulan Sorular](#11-sık-sorulan-sorular)

---

## Giriş

**AI Asistan**, kurumsal iş zekası platformudur. Tek bir sohbet arayüzünden:

- **Genel sohbet** — Sorularınızı sorun, AI yanıtlasın
- **Döküman arama** — Sisteme yüklediğiniz belgelerde akıllı arama
- **Rapor oluşturma** — Doğal dil ile veritabanı sorguları ve interaktif dashboard'lar
- **Dosya analizi** — Excel/CSV dosyalarınızı sürükleyip analiz ettirin

---

## 1. Giriş Yapma ve Kayıt

### 1.1 Email ile Giriş

1. `/login` sayfasına gidin
2. **"Giriş Yap"** sekmesinin seçili olduğundan emin olun
3. E-posta adresinizi ve şifrenizi girin
4. **"Beni hatırla"** seçeneğini isterseniz işaretleyin
5. **"Giriş Yap"** butonuna tıklayın

### 1.2 Windows ile Giriş (Active Directory SSO)

Kurumsal ağdaysanız:

1. `/login` sayfasında **"Windows ile Giriş"** butonuna tıklayın
2. Tarayıcı otomatik olarak Windows kimlik bilgilerinizi kullanır
3. İlk girişte hesabınız otomatik oluşturulur

> **Not:** Windows girişi sadece kurumsal ağ üzerinden çalışır.

### 1.3 Yeni Hesap Oluşturma

1. **"Kayıt Ol"** sekmesine geçin
2. Ad Soyad, E-posta, Şifre ve Şifre Tekrar alanlarını doldurun
3. Şifre güçlük göstergesi **"Güçlü şifre"** olana kadar şifrenizi güçlendirin
4. **"Kayıt Ol"** butonuna tıklayın
5. Başarılı kayıt sonrası giriş yapabilirsiniz

### 1.4 Çıkış Yapma

- Sağ üst köşedeki profil ikonuna tıklayın → **"Çıkış Yap"**

---

## 2. Ana Ekran (Chat)

Giriş yaptıktan sonra ana sohbet ekranı açılır:

```
┌──────────────────────────────────────────────────────────┐
│  📂 Sidebar  │            Ana Chat Alanı                 │
│              │                                           │
│  Sohbet      │  ┌─────────────────────────────────────┐  │
│  Geçmişi     │  │       Hoş Geldiniz Mesajı           │  │
│              │  │                                     │  │
│  ┌────────┐  │  │  [💬 Chat]  [📄 Döküman]  [📊 Rapor]│  │
│  │Yeni    │  │  │                                     │  │
│  │Sohbet  │  │  └─────────────────────────────────────┘  │
│  └────────┘  │                                           │
│              │  ┌───────────────────────────────────┐    │
│  🔍 Ara...   │  │ 📎 │ Mesajınızı yazın...          │    │
│              │  │    │ # ile filtre ekleyin         │    │
│  ├ Sohbet 1  │  └───────────────────────────────────┘    │
│  ├ Sohbet 2  │  [#] ile filtre  [Shift+Enter] yeni       │
│  └ Sohbet 3  │  satır  [Enter] ile gönderin              │
└──────────────────────────────────────────────────────────┘
```

### Bileşenler

| Alan | Açıklama |
|------|----------|
| **Sidebar (Sol Panel)** | Sohbet geçmişi, arama, yeni sohbet butonu |
| **Header (Üst Bar)** | Logo, bağlantı durumu, profil menüsü |
| **Chat Alanı (Orta)** | Mesajlar, AI yanıtları, öneriler |
| **Giriş Alanı (Alt)** | Mesaj yazma, dosya ekleme, filtre, gönderme |

### Hoş Geldiniz Seçenekleri

İlk açılışta üç seçenek sunulur:

| Seçenek | Açıklama |
|---------|----------|
| **💬 Chat** | Genel sorularınız ve sohbet için |
| **📄 Döküman İşlemleri** | Sisteme yüklenen belgelerde arama |
| **📊 Rapor İşlemleri** | Veri analizi ve rapor oluşturma |

Bir seçeneğe tıklamak o konuya uygun sohbeti başlatır.

---

## 3. Sohbet İşlemleri

### 3.1 Mesaj Gönderme

1. Alt kısımdaki metin alanına mesajınızı yazın
2. **Enter** tuşuna basın veya ➤ butonuna tıklayın
3. AI yanıtı gerçek zamanlı olarak **streaming** şeklinde gelir

> **İpucu:** Birden fazla satır yazmak için **Shift+Enter** kullanın.

### 3.2 Dosya Ekleyerek Soru Sorma

Chat'e dosya ekleyerek analiz yaptırabilirsiniz:

1. Giriş alanındaki 📎 (ataç) butonuna tıklayın
2. Desteklenen dosya formatlarından birini seçin:
   - **Excel:** `.xlsx`, `.xls`
   - **CSV:** `.csv`
   - **PDF:** `.pdf`
   - **Word:** `.docx`, `.doc`
   - **Metin:** `.txt`
   - **PowerPoint:** `.pptx`, `.ppt`
3. Dosya adı giriş alanının üstünde görünür
4. Mesajınızı yazıp gönderin — AI dosyayı analiz ederek yanıt verir
5. Dosyayı kaldırmak için ✕ butonuna tıklayın

### 3.3 Öneri Butonları

AI yanıtlarının altında **"Öneriler"** bölümü çıkabilir. Bu öneriler:

- İlgili takip soruları sunar
- Tıklandığında otomatik olarak o soru gönderilir
- Konuyu derinleştirmek için kullanışlıdır

### 3.4 AI Düşünme Süreci (ReAct Panel)

Karmaşık sorularda AI'ın düşünme adımlarını görebilirsiniz:

1. Mesaj gönderildikten sonra **"AI Düşünme Süreci (N)"** butonu belirir
2. Tıklayarak adımları açıp kapatabilirsiniz:
   - 💭 **Düşünce** — AI'ın analiz süreci
   - ⚡ **Aksiyon** — AI'ın aldığı aksiyon
   - 👁️ **Gözlem** — AI'ın sonuçları değerlendirmesi

### 3.5 Sohbet Geçmişi Yönetimi

**Yeni Sohbet:**

- Sidebar'daki **"+ Yeni Sohbet"** butonuna tıklayın

**Geçmiş Sohbete Dönme:**

- Sidebar'daki sohbet listesinden bir sohbete tıklayın

**Sohbet Başlığını Düzenleme:**

- Sohbetin üzerine gelin → ✏️ ikonuna tıklayın
- Yeni başlığı girin → **"Kaydet"**

**Sohbet Silme:**

- Sohbetin üzerine gelin → 🗑️ ikonuna tıklayın
- Onay diyalogunda **"Sil"** butonuna tıklayın

> ⚠️ **Uyarı:** Silme işlemi geri alınamaz!

**Sohbet Arama:**

- Sidebar'daki arama kutusuna yazarak sohbetleri filtreleyebilirsiniz

---

## 4. Döküman Yönetimi

Sağ üst köşedeki profil menüsünden erişilir.

### 4.1 Kategori Oluşturma

Dökümanları organize etmek için önce kategori oluşturun:

1. Profil menüsü → **"Kategori Ekle"**
2. **Kategori ID:** Benzersiz, küçük harf, tire kullanabilir (örn: `satis-raporlari`)
3. **Görünen Ad:** Kullanıcı dostu isim (örn: "Satış Raporları")
4. **Açıklama:** (Opsiyonel)
5. **"Kategori Oluştur"** butonuna tıklayın

### 4.2 Döküman Yükleme

1. Profil menüsü → **"Döküman Yükle"**
2. Dosya seçimi — iki yöntem:
   - **Sürükle-Bırak:** Dosyayı yükleme alanına sürükleyin
   - **Dosya Seç:** Yükleme alanına tıklayıp dosya seçin
3. Desteklenen formatlar: **PDF, TXT, DOCX, DOC, JSON** (Maks. 50MB)
4. **Görünen Ad:** Dökümanın listede görünecek adı
5. **Kategori:** Dökümanı bir kategoriye atayın
6. **Döküman Tipi:**
   - **Döküman** — Genel belge (metin tabanlı)
   - **Soru-Cevap** — S/C formatında bilgi
7. **Açıklama:** (Opsiyonel)
8. **"Yükle"** butonuna tıklayın

> **Arka Planda:** Yüklenen dosya otomatik olarak ayrıştırılır (parse), parçalara bölünür (chunk) ve vektör veritabanına gömülür (embed). Bu süreç dosya boyutuna göre birkaç saniye sürebilir.

### 4.3 Dökümanlarım (Liste)

1. Profil menüsü → **"Dökümanlarım"**
2. Tablo görünümünde dökümanlarınızı görün:
   - **Ad, Tip, Kategori, Durum, Tarih**
3. Arama ve filtreleme:
   - Üst kısımdaki arama kutusuyla döküman arayın
   - Kategori filtresiyle daraltın
4. İşlemler:
   - Detay görüntüleme
   - Döküman silme
5. **"Yeni Yükle"** butonu ile direkt yükleme modalını açın

### 4.4 Döküman Arama (Chat'te)

Yüklenen dökümanlar üzerinde akıllı arama yapabilirsiniz:

1. Chat ekranında doğrudan sorunuzu yazın:

   ```
   Satış politikası hakkında ne bilgi var?
   ```

2. Veya `#` ile filtreleyerek spesifik döküman/kategori seçin (bkz. [Filtreleme Sistemi](#6-filtreleme-sistemi))
3. AI, ilgili dökümanlardan bilgi bularak yanıt verir
4. Yanıtta kaynak döküman referansları gösterilir

---

## 5. Rapor İşlemleri

### 5.1 Doğal Dil ile Rapor Oluşturma

Veritabanı raporu almak için chat'e doğal dilde yazın:

```
Son 6 ayın satış trendini göster
```

```
En çok satan 10 ürünü listele
```

```
Müşteri bazlı gelir analizi yap
```

AI otomatik olarak:

1. SQL sorgusu oluşturur
2. Sorguyu doğrular ve optimize eder
3. Veritabanında çalıştırır
4. Sonuçları **interaktif HTML dashboard** olarak sunar

### 5.2 Dashboard Görüntüleme

Rapor hazır olduğunda ekranda bir dashboard penceresi açılır:

| İşlem | Açıklama |
|-------|----------|
| **Küçült** | Dashboard'u alt köşeye küçültür |
| **Yeni sekmede aç** | Dashboard'u tarayıcı sekmesinde açar |
| **Kapat** | Dashboard'u kapatır |

### 5.3 Birden Fazla Rapor

Birden fazla rapor açabilirsiniz:

- Küçültülen raporlar ekranın alt köşesinde ikon olarak görünür
- **1 rapor:** Tek bir minimize kartı
- **Birden fazla:** Klasör stilinde gruplanır
- Klasöre tıklayarak rapor listesini açabilirsiniz
- **"Tümünü Kapat"** ile hepsini kapatabilirsiniz

### 5.4 Raporu Zamanlama

Oluşturulan bir raporu otomatik çalışacak şekilde zamanlayabilirsiniz:

1. Rapor mesajının altındaki **"⏰ Zamanla"** butonuna tıklayın
2. Zamanlama modalı açılır:
   - **Rapor Adı:** Rapora anlamlı bir isim verin
   - **Açıklama:** (Opsiyonel)
3. **Hızlı Seçim** butonları:

   | Buton | Zamanlama |
   |-------|----------|
   | **Hafta içi 09:00** | Pazartesi-Cuma, her sabah 09:00 |
   | **Her gün 09:00** | Her gün sabah 09:00 |
   | **Haftalık (Pzt)** | Her Pazartesi 09:00 |
   | **Aylık (1.)** | Her ayın 1'inde 09:00 |

4. **Özel Zamanlama** için:
   - **Sıklık:** Her gün / Hafta içi / Haftada bir / Ayda bir
   - **Gün:** (Haftalık seçimde) Pazartesi-Pazar
   - **Ayın Günü:** (Aylık seçimde) 1-28
   - **Saat:** 00:00-23:00
5. **"Zamanla"** butonuna tıklayın

> Zamanlanmış raporlar belirlenen zamanda otomatik çalışır ve sonuçlar kaydedilir.

---

## 6. Filtreleme Sistemi

Chat giriş alanında `#` yazarak gelişmiş filtreleme yapabilirsiniz.

### 6.1 Filtre Ekleme

1. Mesaj alanına `#` yazın
2. Kategori menüsü açılır:
   - Döküman kategorileri
   - Döküman seçimi
   - Tarih filtresi
   - Diğer filtreler
3. Bir kategori seçin → seçim modalı açılır
4. Değeri seçin → filtre etiketi eklenir
5. Birden fazla filtre ekleyebilirsiniz

### 6.2 Tarih Filtreleri

Tarih filtresi seçildiğinde özel bir modal açılır:

| Mod | Açıklama |
|-----|----------|
| **⚡ Hızlı Seçim** | Bugün, Dün, Son 7 gün, Son 30 gün, Bu ay, Geçen ay |
| **📅 Tek Tarih** | Belirli bir tarih seçin |
| **📅 Tarih Aralığı** | Başlangıç ve bitiş tarihi |
| **📅 Ay Seçimi** | Yıl ve ay seçin |

### 6.3 Filtre Etiketleri

Eklenen filtreler giriş alanının üstünde renkli etiketler olarak görünür:

- Her etiketin yanında ✕ butonu → tek filtreyi kaldırır
- **"Tümünü Temizle"** → tüm filtreleri kaldırır

### 6.4 Örnek Kullanım

```
#kategori:satis-raporlari #tarih:son-30-gun Satış trendleri nedir?
```

Bu sorgu: "satis-raporlari" kategorisindeki dökümanlardan, son 30 gündeki verilere göre satış trendlerini arar.

---

## 7. Geri Bildirim (Feedback)

AI yanıtlarının kalitesini değerlendirebilirsiniz:

### 7.1 Olumlu Geri Bildirim

- AI yanıtının altındaki 👍 butonuna tıklayın
- Yanıt "faydalı" olarak işaretlenir

### 7.2 Olumsuz Geri Bildirim

1. AI yanıtının altındaki 👎 butonuna tıklayın
2. Yorum modalı açılır:
   - **"Bu yanıtı neden beğenmediniz?"** sorusu
   - Yorumunuzu yazın (maks. 500 karakter, opsiyonel)
3. **"Gönder"** ile yorumlu feedback gönderin
4. Veya **"Atla"** ile yorumsuz feedback gönderin

> **Geri bildirimleriniz** AI'ı geliştirmek için kullanılır. Sistem her gece saat 02:00'de tüm feedback'leri AI ile analiz eder.

---

## 8. Dashboard (Analitik)

Feedback analitik sayfasına erişim:

1. Profil menüsü → **"Dashboard"**
2. Veya doğrudan `/dashboard` URL'sine gidin

### İçerik

| Bölüm | Açıklama |
|-------|----------|
| **Genel İstatistikler** | Toplam geri bildirim, memnuniyet oranı, bekleyen iyileştirme, uygulanan iyileştirme |
| **Trend Grafikleri** | Günlük pozitif/negatif feedback trendi |
| **Kategori Dağılımı** | Feedback kategorileri (hangi konularda sorun var) |
| **İyileştirme Önerileri** | AI tarafından üretilen prompt iyileştirme önerileri |
| **Analiz Raporları** | Günlük AI analiz raporları listesi |

### Zaman Aralığı

Sağ üst köşedeki dropdown ile:

- Son 7 gün
- Son 14 gün
- Son 30 gün
- Son 90 gün

---

## 9. Hazır Raporlar (AdventureWorks)

Önceden hazırlanmış interaktif raporlara doğrudan erişebilirsiniz:

| Rapor | URL | Açıklama |
|-------|-----|----------|
| **En Çok Satan Ürünler** | `/reports/adventureworks/top-products` | Ürün satış sıralaması |
| **En İyi Müşteriler** | `/reports/adventureworks/top-customers` | Müşteri bazlı gelir |
| **Aylık Satış Trendi** | `/reports/adventureworks/monthly-sales-trend` | Aylık satış grafiği |
| **Ürün Karlılığı** | `/reports/adventureworks/product-category-profitability` | Kategori bazlı kar |
| **Düşük Stok Uyarısı** | `/reports/adventureworks/low-stock-alert` | Stok seviyesi düşük ürünler |
| **Çalışan Dağılımı** | `/reports/adventureworks/employee-department-distribution` | Departman bazlı çalışan |

Bu raporlar canlı veritabanından veri çeker ve interaktif HTML dashboard'lar olarak görüntülenir.

---

## 10. Klavye Kısayolları

| Kısayol | İşlev |
|---------|-------|
| `Enter` | Mesaj gönder |
| `Shift + Enter` | Yeni satır |
| `#` | Filtre menüsünü aç |
| `Esc` | Açık modal/menüyü kapat |

---

## 11. Sık Sorulan Sorular

### Hangi dosya formatlarını yükleyebilirim?

- **Döküman Yükleme (RAG):** PDF, TXT, DOCX, DOC, JSON (maks. 50MB)
- **Chat'te Dosya Analizi:** Excel (XLSX, XLS), CSV, PDF, DOCX, DOC, TXT, PPTX, PPT

### AI ne kadar sürede yanıt verir?

- **Basit sorular:** 2-5 saniye
- **Döküman arama:** 3-8 saniye
- **Rapor oluşturma:** 10-30 saniye (SQL üretme + çalıştırma + dashboard)
- Yanıtlar **streaming** olarak gelir, beklemenize gerek kalmaz

### Sohbet geçmişim ne kadar saklanır?

Tüm sohbetleriniz veritabanında kalıcı olarak saklanır. Sidebar'dan istediğiniz zaman eski sohbetlerinize dönebilirsiniz.

### Birden fazla dosya ekleyebilir miyim?

Chat'te aynı anda **tek dosya** ekleyebilirsiniz. Yeni dosya seçtiğinizde önceki dosya değiştirilir.

### Windows girişi çalışmıyor, ne yapmalıyım?

- Kurumsal ağ üzerinde olduğunuzdan emin olun
- VPN bağlantınızı kontrol edin
- Tarayıcınızın Windows Authentication'ı desteklediğinden emin olun (Chrome, Edge önerilir)

### Filtreleri nasıl kaldırırım?

- Tek filtre: Etiketin yanındaki ✕ butonuna tıklayın
- Tümü: **"Tümünü Temizle"** butonuna tıklayın

### Zamanlanmış raporlarımı nereden yönetirim?

Zamanlanmış raporlar chat arayüzü üzerinden oluşturulur ve yönetilir. Rapor mesajının altındaki "Zamanla" butonu ile zamanlama ayarlarını yapabilirsiniz.

---

## Bağlantı Durumu

Sağ üst köşedeki bağlantı göstergesi:

| Durum | Anlam |
|-------|-------|
| 🟢 **Bağlı** | Sunucu ile gerçek zamanlı bağlantı aktif |
| 🟡 **Bağlanıyor** | Bağlantı kuruluyor |
| 🔴 **Bağlantı Kesildi** | Sunucu ile bağlantı yok — otomatik yeniden bağlanma denenir |

---

## İlgili Dökümanlar

| Döküman | Açıklama |
|---------|----------|
| [System-Overview.md](System-Overview.md) | Genel sistem mimarisi |
| [Chat-System.md](Chat-System.md) | Chat sistemi teknik detayları |
| [Authentication-Authorization.md](Authentication-Authorization.md) | Kimlik doğrulama detayları |
| [Scheduled-Reports.md](Scheduled-Reports.md) | Zamanlanmış rapor teknik detayları |
| [Infrastructure-Cross-Cutting.md](Infrastructure-Cross-Cutting.md) | Rate limiting ve güvenlik |

---
