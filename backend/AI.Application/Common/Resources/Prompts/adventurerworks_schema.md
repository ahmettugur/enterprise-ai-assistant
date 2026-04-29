# AdventureWorks2022 Veritabanı - Tüm Tablolar Kataloğu

---

## Sales Şeması - Satış İşlemleri

### CountryRegionCurrency Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Sales |
| **Tablo Adı** | CountryRegionCurrency |
| **Tablo Açıklaması** | Ülke ve para birimi arasındaki ilişkileri yöneten tablo. Hangi ülkede hangi para biriminin kullanıldığını belirler. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| CountryRegionCode | nvarchar(3) | Ülke Kodu | Ülke/bölge kodu (CountryRegion tablosuna Foreign Key) |
| CurrencyCode | nvarchar(3) | Para Birimi Kodu | Para birimi kodu (Currency tablosuna Foreign Key) |
| ModifiedDate | datetime | Değiştirilme Tarihi | Kaydın son güncellenme tarihi |

---

### CreditCard Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Sales |
| **Tablo Adı** | CreditCard |
| **Tablo Açıklaması** | Müşterilerin kredi kartı bilgilerini saklayan tablo. Kart numarası, türü ve son kullanma tarihi gibi bilgileri içerir. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| CardNumber | nvarchar(25) | Kart Numarası | Kredi kartı numarası (şifrelenmiş format) |
| CardType | nvarchar(50) | Kart Türü | Kredi kartı türü (Visa, MasterCard, American Express vb.) |
| CreditCardID | int | Kredi Kartı Numarası | Kredi kartı benzersiz kimlik numarası (Primary Key) |
| ExpMonth | tinyint | Son Kullanma Ayı | Kartın son kullanma ayı (1-12 arası) |
| ExpYear | smallint | Son Kullanma Yılı | Kartın son kullanma yılı |
| ModifiedDate | datetime | Değiştirilme Tarihi | Kaydın son güncellenme tarihi |

---

### Currency Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Sales |
| **Tablo Adı** | Currency |
| **Tablo Açıklaması** | Sistemde kullanılan para birimlerinin temel bilgilerini içeren tablo. Para birimi kodları ve isimleri saklanır. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| CurrencyCode | nvarchar(3) | Para Birimi Kodu | Para birimi kodu (ISO 4217 standardında 3 karakterli kod - örn: USD, EUR, TRY) |
| ModifiedDate | datetime | Değiştirilme Tarihi | Kaydın son güncellenme tarihi |
| Name | nvarchar(50) | Para Birimi Adı | Para biriminin tam adı (örn: US Dollar, Euro, Turkish Lira) |

---

### CurrencyRate Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Sales |
| **Tablo Adı** | CurrencyRate |
| **Tablo Açıklaması** | Farklı para birimleri arasındaki döviz kurlarını ve tarihsel değişimlerini saklayan tablo. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| AverageRate | money | Ortalama Kur | Günlük ortalama döviz kuru oranı |
| CurrencyRateDate | datetime | Kur Tarihi | Döviz kurunun geçerli olduğu tarih |
| CurrencyRateID | int | Kur Numarası | Döviz kuru kaydının benzersiz kimlik numarası (Primary Key) |
| EndOfDayRate | money | Gün Sonu Kuru | Gün sonu kapanış döviz kuru oranı |
| FromCurrencyCode | nvarchar(3) | Kaynak Para Birimi | Kaynak para birimi kodu (hangi para biriminden) |
| ModifiedDate | datetime | Değiştirilme Tarihi | Kaydın son güncellenme tarihi |
| ToCurrencyCode | nvarchar(3) | Hedef Para Birimi | Hedef para birimi kodu (hangi para birimine) |

---

### Customer Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Sales |
| **Tablo Adı** | Customer |
| **Tablo Açıklaması** | Müşterilerin temel bilgilerini içeren ana tablo. **İki tip müşteri destekler:** (1) **Bireysel müşteriler (B2C)**: PersonID üzerinden Person.Person tablosuna bağlanır, StoreID NULL'dır. (2) **Kurumsal müşteriler (B2B)**: StoreID üzerinden Sales.Store tablosuna bağlanır (bayi/mağaza), PersonID NULL'dır. Her müşteri bir TerritoryID ile satış bölgesine atanır. SalesOrderHeader tablosuyla CustomerID üzerinden sipariş ilişkisi kurulur. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| AccountNumber | nvarchar(10) | Hesap Numarası | Müşteri hesap numarası (otomatik oluşturulan benzersiz kod) |
| CustomerID | int | Müşteri Numarası | Müşteri benzersiz kimlik numarası (Primary Key) |
| ModifiedDate | datetime | Değiştirilme Tarihi | Kaydın son güncellenme tarihi |
| PersonID | int | Kişi Numarası | Bireysel müşteri için kişi kimliği (Person tablosuna Foreign Key) |
| rowguid | uniqueidentifier | Satır Tanımlayıcısı | Satır benzersiz tanımlayıcısı (GUID) |
| StoreID | int | Mağaza Numarası | Kurumsal müşteri için mağaza kimliği (Store tablosuna Foreign Key) |
| TerritoryID | int | Bölge Numarası | Müşterinin bağlı olduğu satış bölgesi kimliği (SalesTerritory tablosuna Foreign Key) |

---

### PersonCreditCard Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Sales |
| **Tablo Adı** | PersonCreditCard |
| **Tablo Açıklaması** | Kişiler ve kredi kartları arasındaki ilişkileri yöneten tablo. Hangi kişinin hangi kredi kartını kullandığını belirler. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| BusinessEntityID | int | İş Varlığı Numarası | Kişi kimlik numarası (Person tablosuna Foreign Key) |
| CreditCardID | int | Kredi Kartı Numarası | Kredi kartı kimlik numarası (CreditCard tablosuna Foreign Key) |
| ModifiedDate | datetime | Değiştirilme Tarihi | Kaydın son güncellenme tarihi |

---

### SalesOrderDetail Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Sales |
| **Tablo Adı** | SalesOrderDetail |
| **Tablo Açıklaması** | Satış siparişlerinin kalem detaylarını içeren tablo. **Ana tablo ilişkisi:** SalesOrderID → SalesOrderHeader (her siparişin birden fazla detay satırı olabilir). **Ürün ilişkisi:** ProductID → Production.Product. **Özel teklifler:** SpecialOfferID ve ProductID birlikte SpecialOfferProduct tablosuna referans verir (indirim kampanyaları). **Tutar hesaplaması:** LineTotal = (OrderQty × UnitPrice) × (1 - UnitPriceDiscount). En çok satılan ürün, kategori bazlı satışlar ve gelir analizleri için temel tablo. CarrierTrackingNumber ile kargo takibi yapılır. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| CarrierTrackingNumber | nvarchar(25) | Kargo Takip Numarası | Kargo takip numarası |
| LineTotal | numeric(19,4) | Satır Tutarı | Satır toplam tutarı (miktar × birim fiyat - indirim) |
| ModifiedDate | datetime | Değiştirilme Tarihi | Kaydın son güncellenme tarihi |
| OrderQty | smallint | Sipariş Miktarı | Sipariş edilen ürün miktarı |
| ProductID | int | Ürün Numarası | Ürün kimlik numarası (Product tablosuna Foreign Key) |
| rowguid | uniqueidentifier | Satır Tanımlayıcısı | Satır benzersiz tanımlayıcısı (GUID) |
| SalesOrderDetailID | int | Sipariş Detay Numarası | Sipariş detay satırı benzersiz kimlik numarası (Primary Key) |
| SalesOrderID | int | Sipariş Numarası | Sipariş kimlik numarası (SalesOrderHeader tablosuna Foreign Key) |
| SpecialOfferID | int | Özel Teklif Numarası | Özel teklif kimlik numarası (SpecialOffer tablosuna Foreign Key) |
| UnitPrice | money | Birim Fiyatı | Birim fiyat |
| UnitPriceDiscount | real | Birim İndirim Yüzdesi | Birim fiyat indirimi (yüzde olarak) |

---

### SalesOrderHeader Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Sales |
| **Tablo Adı** | SalesOrderHeader |
| **Tablo Açıklaması** | Satış siparişlerinin ana bilgilerini içeren merkezi tablo. **Müşteri ilişkisi:** CustomerID → Customer tablosu (bireysel veya kurumsal). **Satış temsilcisi:** SalesPersonID → SalesPerson (NULL ise online sipariş). **Adresler:** BillToAddressID (fatura) ve ShipToAddressID (teslimat) → Person.Address. **Sipariş kanalı:** OnlineOrderFlag=1 ise web sitesinden, 0 ise satış temsilcisi aracılığıyla. **Durum (Status):** 1=İşlemde, 2=Onaylandı, 3=Sevkiyata Hazır, 4=Reddedildi, 5=Sevk Edildi, 6=İptal. **Detay satırları:** SalesOrderDetail tablosunda ürün kalemleri saklanır. TerritoryID ile bölgesel satış raporları oluşturulur. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| AccountNumber | nvarchar(15) | Hesap Numarası | Müşteri hesap numarası |
| BillToAddressID | int | Fatura Adresi Numarası | Fatura adresi kimliği (Address tablosuna Foreign Key) |
| Comment | nvarchar(MAX) | Açıklama | Sipariş yorumu/açıklaması |
| CreditCardApprovalCode | varchar(15) | Kredi Kartı Onay Kodu | Kredi kartı onay kodu |
| CreditCardID | int | Kredi Kartı Numarası | Kredi kartı kimliği (CreditCard tablosuna Foreign Key) |
| CurrencyRateID | int | Döviz Kuru Numarası | Döviz kuru kimliği (CurrencyRate tablosuna Foreign Key) |
| CustomerID | int | Müşteri Numarası | Müşteri kimliği (Customer tablosuna Foreign Key) |
| DueDate | datetime | Teslim Tarihi | Sipariş teslim tarihi |
| Freight | money | Kargo Ücreti | Kargo ücreti |
| ModifiedDate | datetime | Değiştirilme Tarihi | Kaydın son güncellenme tarihi |
| OnlineOrderFlag | bit | Çevrimiçi Sipariş Bayrağı | Online sipariş olup olmadığını belirten bayrak |
| OrderDate | datetime | Sipariş Tarihi | Sipariş tarihi |
| PurchaseOrderNumber | nvarchar(25) | Satın Alma Siparişi | Satın alma sipariş numarası |
| RevisionNumber | tinyint | Revizyon Numarası | Revizyon numarası |
| rowguid | uniqueidentifier | Satır Tanımlayıcısı | Satır benzersiz tanımlayıcısı (GUID) |
| SalesOrderID | int | Sipariş Numarası | Sipariş benzersiz kimlik numarası (Primary Key) |
| SalesOrderNumber | nvarchar(25) | Sipariş Referans Numarası | Sipariş numarası (otomatik oluşturulan) |
| SalesPersonID | int | Satış Temsilcisi Numarası | Satış temsilcisi kimliği (SalesPerson tablosuna Foreign Key) |
| ShipDate | datetime | Kargo Tarihi | Kargo tarihi |
| ShipMethodID | int | Kargo Yöntemi Numarası | Kargo yöntemi kimliği (ShipMethod tablosuna Foreign Key) |
| ShipToAddressID | int | Teslimat Adresi Numarası | Teslimat adresi kimliği (Address tablosuna Foreign Key) |
| Status | tinyint | Durum | Sipariş durumu |
| SubTotal | money | Ara Toplam | Ara toplam (vergiler hariç) |
| TaxAmt | money | Vergi Tutarı | Vergi tutarı |
| TerritoryID | int | Bölge Numarası | Satış bölgesi kimliği (SalesTerritory tablosuna Foreign Key) |
| TotalDue | money | Toplam Tutarı | Toplam ödenecek tutar |

---

### SalesOrderHeaderSalesReason Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Sales |
| **Tablo Adı** | SalesOrderHeaderSalesReason |
| **Tablo Açıklaması** | Satış siparişleri ve satış nedenleri arasındaki ilişkileri yöneten tablo. Siparişlerin hangi nedenlerle verildiğini belirler. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| ModifiedDate | datetime | Değiştirilme Tarihi | Kaydın son güncellenme tarihi |
| SalesOrderID | int | Sipariş Numarası | Sipariş kimliği (SalesOrderHeader tablosuna Foreign Key) |
| SalesReasonID | int | Satış Nedeni Numarası | Satış nedeni kimliği (SalesReason tablosuna Foreign Key) |

---

### SalesPerson Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Sales |
| **Tablo Adı** | SalesPerson |
| **Tablo Açıklaması** | Satış temsilcilerinin performans ve hedef bilgilerini içeren tablo. **Hiyerarşi:** SalesPerson.BusinessEntityID → HumanResources.Employee → Person.Person (kişi bilgileri için). Her satış temsilcisi bir SalesTerritory'ye atanır. **İlişkiler:** (1) SalesOrderHeader.SalesPersonID ile siparişlere bağlanır (2) Store.SalesPersonID ile mağaza/bayi sorumluluğu belirlenir (3) SalesPersonQuotaHistory ile hedef geçmişi izlenir. SalesYTD ve SalesLastYear alanları performans karşılaştırması için kullanılır. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| Bonus | money | Bonus Tutarı | Bonus tutarı |
| BusinessEntityID | int | İş Varlığı Numarası | İş varlığı kimliği (Person tablosuna Foreign Key) |
| CommissionPct | smallmoney | Komisyon Yüzdesi | Komisyon yüzdesi |
| ModifiedDate | datetime | Değiştirilme Tarihi | Kaydın son güncellenme tarihi |
| rowguid | uniqueidentifier | Satır Tanımlayıcısı | Satır benzersiz tanımlayıcısı (GUID) |
| SalesLastYear | money | Geçen Yıl Satışı | Geçen yıl satış tutarı |
| SalesQuota | money | Satış Hedefi | Satış hedefi/kotası |
| SalesYTD | money | Yıl Başından Satış | Yıl başından bu yana satış tutarı |
| TerritoryID | int | Bölge Numarası | Satış bölgesi kimliği (SalesTerritory tablosuna Foreign Key) |

---

### SalesPersonQuotaHistory Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Sales |
| **Tablo Adı** | SalesPersonQuotaHistory |
| **Tablo Açıklaması** | Satış temsilcilerinin hedef geçmişini saklayan tablo. Zaman içindeki satış hedefi değişimlerini takip eder. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| BusinessEntityID | int | İş Varlığı Numarası | İş varlığı kimliği (SalesPerson tablosuna Foreign Key) |
| ModifiedDate | datetime | Değiştirilme Tarihi | Kaydın son güncellenme tarihi |
| QuotaDate | datetime | Hedef Tarihi | Hedef tarihi |
| rowguid | uniqueidentifier | Satır Tanımlayıcısı | Satır benzersiz tanımlayıcısı (GUID) |
| SalesQuota | money | Satış Hedefi | Satış hedefi/kotası |

---

### SalesReason Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Sales |
| **Tablo Adı** | SalesReason |
| **Tablo Açıklaması** | Satış nedenlerini tanımlayan tablo. Müşterilerin neden satın aldığını kategorize eder. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| ModifiedDate | datetime | Değiştirilme Tarihi | Kaydın son güncellenme tarihi |
| Name | nvarchar(50) | Satış Nedeni Adı | Satış nedeni adı |
| ReasonType | nvarchar(50) | Neden Türü | Neden türü |
| SalesReasonID | int | Satış Nedeni Numarası | Satış nedeni benzersiz kimlik numarası (Primary Key) |

---

### SalesTaxRate Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Sales |
| **Tablo Adı** | SalesTaxRate |
| **Tablo Açıklaması** | Satış vergi oranlarını yöneten tablo. Farklı eyalet ve bölgeler için vergi oranlarını saklar. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| ModifiedDate | datetime | Değiştirilme Tarihi | Kaydın son güncellenme tarihi |
| Name | nvarchar(50) | Vergi Oranı Adı | Vergi oranı adı |
| rowguid | uniqueidentifier | Satır Tanımlayıcısı | Satır benzersiz tanımlayıcısı (GUID) |
| SalesTaxRateID | int | Vergi Oranı Numarası | Satış vergi oranı benzersiz kimlik numarası (Primary Key) |
| StateProvinceID | int | Eyalet Numarası | Eyalet/il kimliği (StateProvince tablosuna Foreign Key) |
| TaxRate | smallmoney | Vergi Oranı Değeri | Vergi oranı (yüzde olarak) |
| TaxType | tinyint | Vergi Türü | Vergi türü |

---

### SalesTerritory Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Sales |
| **Tablo Adı** | SalesTerritory |
| **Tablo Açıklaması** | Satış bölgelerinin performans ve coğrafi bilgilerini içeren tablo. **Bölge Grupları (Group):** North America, Europe, Pacific. **Örnek bölgeler:** Northwest, Northeast, Central, Southwest, Southeast (ABD), Canada, France, Germany, Australia, United Kingdom. **İlişkiler:** (1) Customer.TerritoryID ile müşteriler bölgeye atanır (2) SalesPerson.TerritoryID ile satış temsilcileri bölgeye atanır (3) SalesOrderHeader.TerritoryID ile siparişler bölgeye kaydedilir (4) SalesTerritoryHistory ile temsilci-bölge atama geçmişi izlenir. Bölgesel satış ve maliyet metrikleri (SalesYTD, CostYTD) karşılaştırma ve raporlama için kullanılır. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| CostLastYear | money | Geçen Yıl Maliyeti | Geçen yıl maliyet |
| CostYTD | money | Yıl Başından Maliyeti | Yıl başından bu yana maliyet |
| CountryRegionCode | nvarchar(3) | Ülke Kodu | Ülke/bölge kodu (CountryRegion tablosuna Foreign Key) |
| Group | nvarchar(50) | Bölge Grubu | Bölge grubu - kıta/bölge bazlı gruplama. Name alanı ile karıştırılmamalıdır. |
| ModifiedDate | datetime | Değiştirilme Tarihi | Kaydın son güncellenme tarihi |
| Name | nvarchar(50) | Bölge Adı | Satış bölgesinin adı. Group alanı ile karıştırılmamalıdır. |
| rowguid | uniqueidentifier | Satır Tanımlayıcısı | Satır benzersiz tanımlayıcısı (GUID) |
| SalesLastYear | money | Geçen Yıl Satışı | Geçen yıl satış tutarı |
| SalesYTD | money | Yıl Başından Satışı | Yıl başından bu yana satış tutarı |
| TerritoryID | int | Bölge Numarası | Bölge benzersiz kimlik numarası (Primary Key) |

---

### SalesTerritoryHistory Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Sales |
| **Tablo Adı** | SalesTerritoryHistory |
| **Tablo Açıklaması** | Satış temsilcilerinin bölge geçmişini saklayan tablo. Hangi temsilcinin hangi dönemde hangi bölgede çalıştığını takip eder. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| BusinessEntityID | int | İş Varlığı Numarası | İş varlığı kimliği (SalesPerson tablosuna Foreign Key) |
| EndDate | datetime | Bitiş Tarihi | Bölgede çalışma bitiş tarihi |
| ModifiedDate | datetime | Değiştirilme Tarihi | Kaydın son güncellenme tarihi |
| rowguid | uniqueidentifier | Satır Tanımlayıcısı | Satır benzersiz tanımlayıcısı (GUID) |
| StartDate | datetime | Başlangıç Tarihi | Bölgede çalışma başlangıç tarihi |
| TerritoryID | int | Bölge Numarası | Bölge kimliği (SalesTerritory tablosuna Foreign Key) |

---

### ShoppingCartItem Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Sales |
| **Tablo Adı** | ShoppingCartItem |
| **Tablo Açıklaması** | Online alışveriş sepeti öğelerini saklayan tablo. Müşterilerin sepetlerine eklediği ürünleri yönetir. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| DateCreated | datetime | Oluşturulma Tarihi | Sepet öğesinin oluşturulma tarihi |
| ModifiedDate | datetime | Değiştirilme Tarihi | Kaydın son güncellenme tarihi |
| ProductID | int | Ürün Numarası | Ürün kimliği (Product tablosuna Foreign Key) |
| Quantity | int | Miktar | Ürün miktarı |
| ShoppingCartID | nvarchar(50) | Sepet Numarası | Alışveriş sepeti kimliği |
| ShoppingCartItemID | int | Sepet Öğesi Numarası | Sepet öğesi benzersiz kimlik numarası (Primary Key) |

---

### SpecialOffer Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Sales |
| **Tablo Adı** | SpecialOffer |
| **Tablo Açıklaması** | Özel teklifleri ve promosyonları yöneten tablo. İndirim oranları, geçerlilik tarihleri ve koşulları saklar. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| Category | nvarchar(50) | Teklif Kategorisi | Teklif kategorisi |
| Description | nvarchar(255) | Teklif Açıklaması | Teklif açıklaması |
| DiscountPct | smallmoney | İndirim Yüzdesi | İndirim yüzdesi |
| EndDate | datetime | Bitiş Tarihi | Teklif bitiş tarihi |
| MaxQty | int | Maksimum Miktar | Maksimum miktar |
| MinQty | int | Minimum Miktar | Minimum miktar |
| ModifiedDate | datetime | Değiştirilme Tarihi | Kaydın son güncellenme tarihi |
| rowguid | uniqueidentifier | Satır Tanımlayıcısı | Satır benzersiz tanımlayıcısı (GUID) |
| SpecialOfferID | int | Özel Teklif Numarası | Özel teklif benzersiz kimlik numarası (Primary Key) |
| StartDate | datetime | Başlangıç Tarihi | Teklif başlangıç tarihi |
| Type | nvarchar(50) | Teklif Türü | Teklif türü |

---

### SpecialOfferProduct Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Sales |
| **Tablo Adı** | SpecialOfferProduct |
| **Tablo Açıklaması** | Özel teklifler ve ürünler arasındaki ilişkileri yöneten tablo. Hangi ürünlerin hangi tekliflerde geçerli olduğunu belirler. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| ModifiedDate | datetime | Değiştirilme Tarihi | Kaydın son güncellenme tarihi |
| ProductID | int | Ürün Numarası | Ürün kimliği (Product tablosuna Foreign Key) |
| rowguid | uniqueidentifier | Satır Tanımlayıcısı | Satır benzersiz tanımlayıcısı (GUID) |
| SpecialOfferID | int | Özel Teklif Numarası | Özel teklif kimliği (SpecialOffer tablosuna Foreign Key) |

---

### Store Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Sales |
| **Tablo Adı** | Store |
| **Tablo Açıklaması** | B2B (kurumsal) müşterilerin mağaza/bayi bilgilerini içeren tablo. **Customer tablosuyla ilişki:** Customer.StoreID → Store.BusinessEntityID (kurumsal müşteriler için). **Satış temsilcisi ataması:** Her mağazaya bir SalesPerson atanır (SalesPersonID). **İletişim kişileri:** Person.BusinessEntityContact tablosu üzerinden mağaza yetkilileri (satın alma sorumlusu, mağaza müdürü vb.) belirlenir. Demographics (XML) alanında yıllık gelir, çalışan sayısı, mağaza alanı gibi işletme bilgileri saklanır. Bireysel müşteriler (B2C) için kullanılmaz - onlar doğrudan Person tablosuna bağlanır. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| BusinessEntityID | int | İş Varlığı Numarası | İş varlığı kimliği (BusinessEntity tablosuna Foreign Key) |
| Demographics | xml | Demografik Bilgiler | Demografik bilgiler (XML formatında) |
| ModifiedDate | datetime | Değiştirilme Tarihi | Kaydın son güncellenme tarihi |
| Name | nvarchar(50) | Mağaza Adı | Mağaza adı |
| rowguid | uniqueidentifier | Satır Tanımlayıcısı | Satır benzersiz tanımlayıcısı (GUID) |
| SalesPersonID | int | Satış Temsilcisi Numarası | Satış temsilcisi kimliği (SalesPerson tablosuna Foreign Key) |

---

## Production Şeması - Üretim & Ürünler

### BillOfMaterials Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Production |
| **Tablo Adı** | BillOfMaterials |
| **Tablo Açıklaması** | Ürün bileşen listelerini yöneten tablo. Hangi ürünün hangi bileşenlerden oluştuğunu ve montaj miktarlarını saklar. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| BillOfMaterialsID | int | Malzeme Listesi Numarası | Malzeme listesi kimliği |
| BOMLevel | smallint | Malzeme Listesi Seviyesi | Malzeme listesi seviyesi |
| ComponentID | int | Bileşen Numarası | Bileşen kimliği |
| EndDate | datetime | Bitiş Tarihi | Bitiş tarihi |
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| PerAssemblyQty | decimal(8,2) | Montaj Başına Miktar | Montaj başına miktar |
| ProductAssemblyID | int | Ürün Montajı Numarası | Ürün montaj kimliği |
| StartDate | datetime | Başlangıç Tarihi | Başlangıç tarihi |
| UnitMeasureCode | nchar(3) | Ölçü Birimi Kodu | Ölçü birimi kodu |

---

### Culture Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Production |
| **Tablo Adı** | Culture |
| **Tablo Açıklaması** | Sistem tarafından desteklenen kültür ve dil bilgilerini içeren tablo. Çok dilli destek için kullanılır. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| CultureID | nchar(6) | Kültür Numarası | Kültür kimliği |
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| Name | nvarchar(50) | Kültür Adı | Kültür adı |

---

### Product Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Production |
| **Tablo Adı** | Product |
| **Tablo Açıklaması** | Ürünlerin ana bilgilerini içeren merkezi tablo. **Kategori hiyerarşisi:** ProductSubcategoryID → ProductSubcategory → ProductCategory (3 seviyeli: Bikes > Mountain Bikes > ürünler). **Ana kategoriler:** Bikes (bisikletler), Components (bileşenler), Clothing (giyim), Accessories (aksesuarlar). **Satış ilişkisi:** SalesOrderDetail.ProductID ile satışlara bağlanır. **Stok yönetimi:** ProductInventory tablosunda lokasyon bazlı stok, SafetyStockLevel ve ReorderPoint stok uyarıları için kullanılır. **Üretim:** MakeFlag=1 ise şirket içi üretilir, 0 ise satın alınır. BillOfMaterials ile ürün-bileşen ilişkisi tanımlanır. **Fiyatlandırma:** ListPrice (satış), StandardCost (maliyet) ile kar marjı hesaplanır. Class: H=Yüksek, M=Orta, L=Düşük. ProductLine: R=Yol, M=Dağ, T=Tur, S=Standart. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| Class | nchar(2) | Sınıf | Sınıf |
| Color | nvarchar(15) | Renk | Renk |
| DaysToManufacture | int | Üretim Günleri | Üretim günü sayısı |
| DiscontinuedDate | datetime | Üretimden Kaldırılma Tarihi | Üretimden kaldırılma tarihi |
| FinishedGoodsFlag | bit | Bitmiş Ürün Bayrak | Bitmiş ürün işareti |
| ListPrice | money | Liste Fiyatı | Liste fiyatı |
| MakeFlag | bit | Üretim Bayrak | Üretim işareti |
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| Name | nvarchar(50) | Ürün Adı | Ürün adı |
| ProductID | int | Ürün Numarası | Ürün kimliği |
| ProductLine | nchar(2) | Ürün Hattı | Ürün hattı |
| ProductModelID | int | Ürün Modeli Numarası | Ürün model kimliği |
| ProductNumber | nvarchar(25) | Ürün Kodu | Ürün numarası |
| ProductSubcategoryID | int | Ürün Alt Kategorisi Numarası | Ürün alt kategori kimliği |
| ReorderPoint | smallint | Yeniden Sipariş Noktası | Yeniden sipariş noktası |
| rowguid | uniqueidentifier | Satır Tanımlayıcısı | Satır GUID |
| SafetyStockLevel | smallint | Güvenlik Stok Seviyesi | Güvenlik stok seviyesi |
| SellEndDate | datetime | Satış Bitiş Tarihi | Satış bitiş tarihi |
| SellStartDate | datetime | Satış Başlangıç Tarihi | Satış başlangıç tarihi |
| Size | nvarchar(5) | Boyut | Boyut |
| SizeUnitMeasureCode | nchar(3) | Boyut Ölçü Birimi Kodu | Boyut ölçü birimi kodu |
| StandardCost | money | Standart Maliyet | Standart maliyet |
| Style | nchar(2) | Stil | Stil |
| Weight | decimal(8,2) | Ağırlık | Ağırlık |
| WeightUnitMeasureCode | nchar(3) | Ağırlık Ölçü Birimi Kodu | Ağırlık ölçü birimi kodu |

---

### ProductCategory Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Production |
| **Tablo Adı** | ProductCategory |
| **Tablo Açıklaması** | Ürünlerin ana kategorilerini tanımlayan tablo. **Sabit kategoriler:** 1=Bikes (bisikletler), 2=Components (bileşenler), 3=Clothing (giyim), 4=Accessories (aksesuarlar). **Hiyerarşi:** ProductCategory → ProductSubcategory → Product (en üst seviye). Kategori bazlı satış raporları, gelir analizleri ve stok değerlendirmeleri için temel tablo. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| Name | nvarchar(50) | Kategori Adı | Kategori adı |
| ProductCategoryID | int | Ürün Kategorisi Numarası | Ürün kategori kimliği |
| rowguid | uniqueidentifier | Satır Tanımlayıcısı | Satır GUID |

---

### ProductSubcategory Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Production |
| **Tablo Adı** | ProductSubcategory |
| **Tablo Açıklaması** | Ürün alt kategorilerini tanımlayan tablo. **Ana kategorilere bağlı alt kategoriler:** Bikes altında: Mountain Bikes, Road Bikes, Touring Bikes. Components altında: Handlebars, Bottom Brackets, Brakes, Chains vb. Clothing altında: Bib-Shorts, Caps, Gloves, Jerseys, Shorts, Socks, Tights, Vests. Accessories altında: Bike Racks, Bike Stands, Bottles, Cages, Cleaners, Fenders, Helmets vb. **İlişkiler:** ProductCategoryID → ProductCategory, Product.ProductSubcategoryID → ProductSubcategory. Alt kategori bazlı ürün filtreleme ve raporlama için kullanılır. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| Name | nvarchar(50) | Alt Kategori Adı | Alt kategori adı |
| ProductCategoryID | int | Ürün Kategorisi Numarası | Ürün kategori kimliği |
| ProductSubcategoryID | int | Ürün Alt Kategorisi Numarası | Ürün alt kategori kimliği |
| rowguid | uniqueidentifier | Satır Tanımlayıcısı | Satır GUID |

---

### UnitMeasure Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Production |
| **Tablo Adı** | UnitMeasure |
| **Tablo Açıklaması** | Ölçü birimlerini tanımlayan tablo. Ürün ölçümleri ve miktarları için standart birimleri yönetir. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| Name | nvarchar(50) | Ölçü Birimi Adı | Ölçü birimi adı |
| UnitMeasureCode | nchar(3) | Ölçü Birimi Kodu | Ölçü birimi kodu |

---

### Location Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Production |
| **Tablo Adı** | Location |
| **Tablo Açıklaması** | Depo ve lokasyon bilgilerini yöneten tablo. Stok yerleşimi ve maliyet oranlarını saklar. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| Availability | decimal(8,2) | Kullanılabilirlik | Kullanılabilirlik |
| CostRate | smallmoney | Maliyet Oranı | Maliyet oranı |
| LocationID | smallint | Lokasyon Numarası | Lokasyon kimliği |
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| Name | nvarchar(50) | Lokasyon Adı | Lokasyon adı |

---

### ProductInventory Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Production |
| **Tablo Adı** | ProductInventory |
| **Tablo Açıklaması** | Lokasyon bazlı ürün stok bilgilerini yöneten tablo. **Ürün ilişkisi:** ProductID → Product. **Lokasyon:** LocationID → Location (depo, atölye, raf vb.). **Stok takibi:** Quantity ile mevcut stok, Shelf ve Bin ile fiziksel konum belirlenir. **Stok uyarıları:** Product.SafetyStockLevel ve Product.ReorderPoint ile karşılaştırılarak düşük stok alarmları oluşturulur. Birden fazla lokasyonda aynı ürün olabilir - toplam stok için SUM(Quantity) GROUP BY ProductID kullanılır. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| Bin | tinyint | Kutu/Bölme | Kutu/bölme |
| LocationID | smallint | Lokasyon Numarası | Lokasyon kimliği |
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| ProductID | int | Ürün Numarası | Ürün kimliği |
| Quantity | smallint | Miktar | Miktar |
| rowguid | uniqueidentifier | Satır Tanımlayıcısı | Satır GUID |
| Shelf | nchar(10) | Raf | Raf |

---

### ProductModel Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Production |
| **Tablo Adı** | ProductModel |
| **Tablo Açıklaması** | Ürün modellerini tanımlayan tablo. Model açıklamaları ve üretim talimatlarını içerir. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| CatalogDescription | xml | Katalog Açıklaması | Katalog açıklaması |
| Instructions | xml | Talimatlar | Talimatlar |
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| Name | nvarchar(50) | Model Adı | Model adı |
| ProductModelID | int | Ürün Modeli Numarası | Ürün model kimliği |
| rowguid | uniqueidentifier | Satır Tanımlayıcısı | Satır GUID |

---

### ProductCostHistory Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Production |
| **Tablo Adı** | ProductCostHistory |
| **Tablo Açıklaması** | Ürün maliyetlerinin tarihsel değişimlerini saklayan tablo. Maliyet analizi ve trend takibi için kullanılır. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| EndDate | datetime | Bitiş Tarihi | Bitiş tarihi |
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| ProductID | int | Ürün Numarası | Ürün kimliği |
| StandardCost | money | Standart Maliyet | Standart maliyet |
| StartDate | datetime | Başlangıç Tarihi | Başlangıç tarihi |

---

### ProductListPriceHistory Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Production |
| **Tablo Adı** | ProductListPriceHistory |
| **Tablo Açıklaması** | Ürün liste fiyatlarının tarihsel değişimlerini saklayan tablo. Fiyat analizi ve trend takibi için kullanılır. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| EndDate | datetime | Bitiş Tarihi | Bitiş tarihi |
| ListPrice | money | Liste Fiyatı | Liste fiyatı |
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| ProductID | int | Ürün Numarası | Ürün kimliği |
| StartDate | datetime | Başlangıç Tarihi | Başlangıç tarihi |

---

### ProductDescription Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Production |
| **Tablo Adı** | ProductDescription |
| **Tablo Açıklaması** | Ürün açıklamalarını saklayan tablo. Farklı dillerde ürün tanımları için kullanılır. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| Description | nvarchar(400) | Açıklama | Açıklama |
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| ProductDescriptionID | int | Ürün Açıklaması Numarası | Ürün açıklama kimliği |
| rowguid | uniqueidentifier | Satır Tanımlayıcısı | Satır GUID |

---

### ProductPhoto Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Production |
| **Tablo Adı** | ProductPhoto |
| **Tablo Açıklaması** | Ürün fotoğraflarını saklayan tablo. Büyük ve küçük boyutlu ürün görsellerini yönetir. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| LargePhoto | varbinary(MAX) | Büyük Fotoğraf | Büyük fotoğraf |
| LargePhotoFileName | nvarchar(50) | Büyük Fotoğraf Dosya Adı | Büyük fotoğraf dosya adı |
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| ProductPhotoID | int | Ürün Fotoğrafı Numarası | Ürün fotoğraf kimliği |
| ThumbNailPhoto | varbinary(MAX) | Küçük Fotoğraf | Küçük fotoğraf |
| ThumbnailPhotoFileName | nvarchar(50) | Küçük Fotoğraf Dosya Adı | Küçük fotoğraf dosya adı |

---

### WorkOrder Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Production |
| **Tablo Adı** | WorkOrder |
| **Tablo Açıklaması** | Üretim iş emirlerini yöneten tablo. **Ürün ilişkisi:** ProductID → Product (üretilecek ürün). **Üretim takibi:** OrderQty (hedef miktar), StockedQty (stoka giren miktar), ScrappedQty (hurda miktar) karşılaştırması ile verimlilik ölçülür. **Hurda analizi:** ScrappedQty > 0 ise ScrapReasonID ile hurda nedeni belirlenir. **Üretim rotası:** WorkOrderRouting tablosu ile üretim adımları (kaynak, boyama, montaj vb.) ve her adımın lokasyonu, süresi, maliyeti izlenir. StartDate, EndDate, DueDate ile üretim planlama ve gecikme analizi yapılır. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| DueDate | datetime | Teslim Tarihi | Teslim tarihi |
| EndDate | datetime | Bitiş Tarihi | Bitiş tarihi |
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| OrderQty | int | Sipariş Miktarı | Sipariş miktarı |
| ProductID | int | Ürün Numarası | Ürün kimliği |
| ScrappedQty | int | Hurda Miktarı | Hurda miktarı |
| ScrapReasonID | smallint | Hurda Nedeni Numarası | Hurda nedeni kimliği |
| StartDate | datetime | Başlangıç Tarihi | Başlangıç tarihi |
| StockedQty | int | Stoklanmış Miktar | Stoklanmış miktar |
| WorkOrderID | int | İş Emri Numarası | İş emri kimliği |

---

### TransactionHistory Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Production |
| **Tablo Adı** | TransactionHistory |
| **Tablo Açıklaması** | Ürün işlem geçmişini saklayan tablo. Stok hareketleri, maliyetler ve işlem türlerini takip eder. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| ActualCost | money | Gerçek Maliyet | Gerçek maliyet |
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| ProductID | int | Ürün Numarası | Ürün kimliği |
| Quantity | int | Miktar | Miktar |
| ReferenceOrderID | int | Referans Siparişi Numarası | Referans sipariş kimliği |
| ReferenceOrderLineID | int | Referans Sipariş Satır Numarası | Referans sipariş satır kimliği |
| TransactionDate | datetime | İşlem Tarihi | İşlem tarihi |
| TransactionID | int | İşlem Numarası | İşlem kimliği |
| TransactionType | nchar(1) | İşlem Türü | İşlem türü |

---

### TransactionHistoryArchive Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Production |
| **Tablo Adı** | TransactionHistoryArchive |
| **Tablo Açıklaması** | Arşivlenmiş işlem geçmişini saklayan tablo. Eski işlem kayıtlarının uzun vadeli saklanması için kullanılır. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| ActualCost | money | Gerçek Maliyet | Gerçek maliyet |
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| ProductID | int | Ürün Numarası | Ürün kimliği |
| Quantity | int | Miktar | Miktar |
| ReferenceOrderID | int | Referans Siparişi Numarası | Referans sipariş kimliği |
| ReferenceOrderLineID | int | Referans Sipariş Satır Numarası | Referans sipariş satır kimliği |
| TransactionDate | datetime | İşlem Tarihi | İşlem tarihi |
| TransactionID | int | İşlem Numarası | İşlem kimliği |
| TransactionType | nchar(1) | İşlem Türü | İşlem türü |

---

### Document Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Production |
| **Tablo Adı** | Document |
| **Tablo Açıklaması** | Ürün ve üretim süreçleriyle ilgili dokümanları yöneten tablo. Teknik belgeler, talimatlar ve dökümanları saklar. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| ChangeNumber | int | Değişiklik Numarası | Değişiklik numarası |
| Document | varbinary(MAX) | Doküman İçeriği | Doküman içeriği |
| DocumentLevel | smallint | Doküman Seviyesi | Doküman seviyesi |
| DocumentNode | hierarchyid | Doküman Düğümü | Doküman düğümü (Primary Key) |
| DocumentSummary | nvarchar(MAX) | Doküman Özeti | Doküman özeti |
| FileExtension | nvarchar(8) | Dosya Uzantısı | Dosya uzantısı |
| FileName | nvarchar(400) | Dosya Adı | Dosya adı |
| FolderFlag | bit | Klasör Bayrak | Klasör işareti |
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| Owner | int | Sahip Numarası | Sahip (BusinessEntityID) |
| Revision | nchar(5) | Revizyon | Revizyon |
| rowguid | uniqueidentifier | Satır Tanımlayıcısı | Satır GUID |
| Status | tinyint | Durum | Durum |
| Title | nvarchar(50) | Başlık | Başlık |

---

### Illustration Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Production |
| **Tablo Adı** | Illustration |
| **Tablo Açıklaması** | Ürün görselleri ve diyagramlarını saklayan tablo. Ürün katalogları ve teknik çizimler için kullanılır. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| Diagram | xml | Diyagram | Diyagram (XML formatında) |
| IllustrationID | int | İllüstrasyon Numarası | İllüstrasyon kimliği (Primary Key) |
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |

---

### ProductDocument Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Production |
| **Tablo Adı** | ProductDocument |
| **Tablo Açıklaması** | Ürünler ve dokümanlar arasındaki ilişkileri yöneten tablo. Ürün teknik belgelerini bağlar. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| DocumentNode | hierarchyid | Doküman Düğümü | Doküman düğümü (Document tablosuna Foreign Key) |
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| ProductID | int | Ürün Numarası | Ürün kimliği (Product tablosuna Foreign Key) |

---

### ProductModelIllustration Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Production |
| **Tablo Adı** | ProductModelIllustration |
| **Tablo Açıklaması** | Ürün modelleri ve görseller arasındaki ilişkileri yöneten tablo. Model görsellerini bağlar. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| IllustrationID | int | İllüstrasyon Numarası | İllüstrasyon kimliği (Illustration tablosuna Foreign Key) |
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| ProductModelID | int | Ürün Modeli Numarası | Ürün model kimliği (ProductModel tablosuna Foreign Key) |

---

### ProductModelProductDescriptionCulture Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Production |
| **Tablo Adı** | ProductModelProductDescriptionCulture |
| **Tablo Açıklaması** | Ürün modelleri için çok dilli açıklamaları yöneten tablo. Farklı kültürler için model açıklamalarını saklar. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| CultureID | nchar(6) | Kültür Numarası | Kültür kimliği (Culture tablosuna Foreign Key) |
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| ProductDescriptionID | int | Ürün Açıklaması Numarası | Ürün açıklama kimliği (ProductDescription tablosuna Foreign Key) |
| ProductModelID | int | Ürün Modeli Numarası | Ürün model kimliği (ProductModel tablosuna Foreign Key) |

---

### ProductProductPhoto Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Production |
| **Tablo Adı** | ProductProductPhoto |
| **Tablo Açıklaması** | Ürünler ve fotoğraflar arasındaki ilişkileri yöneten tablo. Hangi fotoğrafın hangi ürüne ait olduğunu belirler. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| Primary | bit | Birincil Fotoğraf | Birincil fotoğraf işareti |
| ProductID | int | Ürün Numarası | Ürün kimliği (Product tablosuna Foreign Key) |
| ProductPhotoID | int | Ürün Fotoğrafı Numarası | Ürün fotoğraf kimliği (ProductPhoto tablosuna Foreign Key) |

---

### ProductReview Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Production |
| **Tablo Adı** | ProductReview |
| **Tablo Açıklaması** | Ürün yorumları ve değerlendirmelerini saklayan tablo. Müşteri geri bildirimlerini yönetir. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| Comments | nvarchar(3850) | Yorumlar | Yorum metni |
| EmailAddress | nvarchar(50) | E-posta Adresi | Yorumcu e-posta adresi |
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| ProductID | int | Ürün Numarası | Ürün kimliği (Product tablosuna Foreign Key) |
| ProductReviewID | int | Ürün Yorum Numarası | Ürün yorum kimliği (Primary Key) |
| Rating | int | Puan | Puan (1-5 arası) |
| ReviewDate | datetime | Yorum Tarihi | Yorum tarihi |
| ReviewerName | nvarchar(50) | Yorumcu Adı | Yorumcu adı |

---

### ScrapReason Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Production |
| **Tablo Adı** | ScrapReason |
| **Tablo Açıklaması** | Hurda nedenlerini tanımlayan tablo. Üretim sürecinde oluşan hurdaların nedenlerini kategorize eder. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| Name | nvarchar(50) | Hurda Nedeni Adı | Hurda nedeni adı |
| ScrapReasonID | smallint | Hurda Nedeni Numarası | Hurda nedeni kimliği (Primary Key) |

---

### WorkOrderRouting Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Production |
| **Tablo Adı** | WorkOrderRouting |
| **Tablo Açıklaması** | İş emirlerinin üretim rotasını tanımlayan tablo. **İş emri ilişkisi:** WorkOrderID → WorkOrder. **Üretim lokasyonu:** LocationID → Location (Tool Crib, Paint Shop, Frame Welding, Final Assembly vb.). **Operasyon sıralaması:** OperationSequence ile üretim adımları sıralanır. **Performans analizi:** Planlanan (ScheduledStartDate, PlannedCost) vs Gerçekleşen (ActualStartDate, ActualCost, ActualResourceHrs) karşılaştırması ile verimlilik ölçülür. Lokasyon bazlı kapasite kullanımı ve darboğaz analizi için kritik tablo. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| ActualCost | money | Gerçek Maliyet | Gerçek maliyet |
| ActualEndDate | datetime | Gerçek Bitiş Tarihi | Gerçek bitiş tarihi |
| ActualResourceHrs | decimal(9,4) | Gerçek Kaynak Saatleri | Gerçek kaynak saatleri |
| ActualStartDate | datetime | Gerçek Başlangıç Tarihi | Gerçek başlangıç tarihi |
| LocationID | smallint | Lokasyon Numarası | Lokasyon kimliği (Location tablosuna Foreign Key) |
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| OperationSequence | smallint | Operasyon Sırası | Operasyon sırası |
| PlannedCost | money | Planlanan Maliyet | Planlanan maliyet |
| ProductID | int | Ürün Numarası | Ürün kimliği (Product tablosuna Foreign Key) |
| ScheduledEndDate | datetime | Planlanan Bitiş Tarihi | Planlanan bitiş tarihi |
| ScheduledStartDate | datetime | Planlanan Başlangıç Tarihi | Planlanan başlangıç tarihi |
| WorkOrderID | int | İş Emri Numarası | İş emri kimliği (WorkOrder tablosuna Foreign Key) |

---

## Person Şeması - Kişi & İletişim Bilgileri

### Person Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Person |
| **Tablo Adı** | Person |
| **Tablo Açıklaması** | Sistemdeki tüm kişilerin temel bilgilerini saklayan merkezi tablo. **PersonType alanına göre kişi türleri:** SC=Mağaza İletişim Kişisi, IN=Bireysel Müşteri, SP=Satış Temsilcisi, EM=Çalışan, VC=Tedarikçi İletişim Kişisi, GC=Genel İletişim Kişisi. **Önemli ilişkiler:** (1) Bireysel müşteriler: Sales.Customer.PersonID → Person.BusinessEntityID (2) Çalışanlar: HumanResources.Employee.BusinessEntityID → Person.BusinessEntityID (3) Satış temsilcileri: Sales.SalesPerson.BusinessEntityID → HumanResources.Employee → Person (4) Mağaza yetkilileri: Person.BusinessEntityContact üzerinden Store'a bağlanır. Ad, soyad, iletişim tercihleri ve demografik bilgileri içerir. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| AdditionalContactInfo | xml | Ek İletişim Bilgileri | Ek iletişim bilgileri |
| BusinessEntityID | int | İş Varlığı Numarası | İş varlığı kimliği |
| Demographics | xml | Demografik Bilgiler | Demografik bilgiler |
| EmailPromotion | int | E-posta Promosyon | E-posta promosyon tercihi |
| FirstName | nvarchar(50) | Ad | Ad |
| LastName | nvarchar(50) | Soyad | Soyad |
| MiddleName | nvarchar(50) | İkinci Ad | İkinci ad |
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| NameStyle | bit | Ad Stili | Ad stili |
| PersonType | nchar(2) | Kişi Türü | Kişi türü |
| rowguid | uniqueidentifier | Satır Tanımlayıcısı | Satır GUID |
| Suffix | nvarchar(10) | Sonek | Sonek |
| Title | nvarchar(8) | Unvan | Unvan |

---

### Address Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Person |
| **Tablo Adı** | Address |
| **Tablo Açıklaması** | Adres bilgilerini saklayan merkezi tablo. **Kullanım alanları:** (1) SalesOrderHeader.BillToAddressID/ShipToAddressID ile sipariş fatura ve teslimat adresleri (2) BusinessEntityAddress tablosu üzerinden kişi, mağaza veya tedarikçi adresleri. **Coğrafi hiyerarşi:** StateProvinceID → StateProvince → CountryRegion ile ülke-eyalet-şehir ilişkisi kurulur. AddressType (BusinessEntityAddress'te) ile adres türü belirlenir: Home, Billing, Shipping, Main Office, Primary vb. SpatialLocation ile coğrafi koordinat bazlı sorgular yapılabilir. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| AddressID | int | Adres Numarası | Adres kimliği |
| AddressLine1 | nvarchar(60) | Adres Satırı 1 | Adres satırı 1 |
| AddressLine2 | nvarchar(60) | Adres Satırı 2 | Adres satırı 2 |
| City | nvarchar(30) | Şehir | Şehir |
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| PostalCode | nvarchar(15) | Posta Kodu | Posta kodu |
| rowguid | uniqueidentifier | Satır Tanımlayıcısı | Satır GUID |
| SpatialLocation | geometry | Mekansal Konum | Mekansal konum |
| StateProvinceID | int | Eyalet Numarası | Eyalet/İl kimliği |

---

### BusinessEntity Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Person |
| **Tablo Adı** | BusinessEntity |
| **Tablo Açıklaması** | Tüm iş varlıkları için ortak kimlik numarası üreten ana tablo. **Bağlı tablolar:** Person.Person (kişiler), Sales.Store (mağazalar), Purchasing.Vendor (tedarikçiler) hepsi aynı BusinessEntityID'yi kullanır. **Amaç:** Farklı tipteki varlıkların (kişi, mağaza, tedarikçi) tek bir kimlik numarası ile tanımlanması ve adres/iletişim bilgilerinin ortak tablolarda saklanması. BusinessEntityAddress ve BusinessEntityContact tabloları bu ID üzerinden çalışır. Identity column olarak otomatik artan numara üretir. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| BusinessEntityID | int | İş Varlığı Numarası | İş varlığı kimliği |
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| rowguid | uniqueidentifier | Satır Tanımlayıcısı | Satır GUID |

---

### EmailAddress Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Person |
| **Tablo Adı** | EmailAddress |
| **Tablo Açıklaması** | E-posta adreslerini saklayan tablo. İş varlıklarının elektronik iletişim bilgilerini yönetir. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| BusinessEntityID | int | İş Varlığı Numarası | İş varlığı kimliği |
| EmailAddress | nvarchar(50) | E-posta Adresi | E-posta adresi |
| EmailAddressID | int | E-posta Adresi Numarası | E-posta adresi kimliği |
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| rowguid | uniqueidentifier | Satır Tanımlayıcısı | Satır GUID |

---

### PersonPhone Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Person |
| **Tablo Adı** | PersonPhone |
| **Tablo Açıklaması** | Kişi telefon bilgilerini saklayan tablo. Birden fazla telefon numarası ve türlerini yönetir. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| BusinessEntityID | int | İş Varlığı Numarası | İş varlığı kimliği |
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| PhoneNumber | nvarchar(25) | Telefon Numarası | Telefon numarası |
| PhoneNumberTypeID | int | Telefon Türü Numarası | Telefon türü kimliği |

---

### StateProvince Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Person |
| **Tablo Adı** | StateProvince |
| **Tablo Açıklaması** | Eyalet ve il bilgilerini saklayan tablo. Ülke içindeki idari bölümleri ve kodlarını yönetir. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| CountryRegionCode | nvarchar(3) | Ülke Kodu | Ülke/Bölge kodu |
| IsOnlyStateProvinceFlag | bit | Sadece Eyalet Bayrağı | Sadece eyalet/il bayrağı |
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| Name | nvarchar(50) | Eyalet/İl Adı | Eyalet/İl adı |
| rowguid | uniqueidentifier | Satır Tanımlayıcısı | Satır GUID |
| StateProvinceCode | nchar(3) | Eyalet Kodu | Eyalet/İl kodu |
| StateProvinceID | int | Eyalet Numarası | Eyalet/İl kimliği |
| TerritoryID | int | Bölge Numarası | Bölge kimliği |

---

### CountryRegion Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Person |
| **Tablo Adı** | CountryRegion |
| **Tablo Açıklaması** | Ülke ve bölge bilgilerini saklayan tablo. Uluslararası adres ve lokasyon verilerini yönetir. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| CountryRegionCode | nvarchar(3) | Ülke Kodu | Ülke/Bölge kodu |
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| Name | nvarchar(50) | Ülke/Bölge Adı | Ülke/Bölge adı |

---

### ContactType Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Person |
| **Tablo Adı** | ContactType |
| **Tablo Açıklaması** | İletişim türlerini tanımlayan tablo. Müşteri, tedarikçi, çalışan gibi iletişim kategorilerini yönetir. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| ContactTypeID | int | İletişim Türü Numarası | İletişim türü kimliği |
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| Name | nvarchar(50) | İletişim Türü Adı | İletişim türü adı |

---

### AddressType Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Person |
| **Tablo Adı** | AddressType |
| **Tablo Açıklaması** | Adres türlerini tanımlayan tablo. Ev, iş, fatura adresi gibi kategorileri yönetir. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| AddressTypeID | int | Adres Türü Numarası | Adres türü kimliği |
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| Name | nvarchar(50) | Adres Türü Adı | Adres türü adı |
| rowguid | uniqueidentifier | Satır Tanımlayıcısı | Satır GUID |

---

### PhoneNumberType Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Person |
| **Tablo Adı** | PhoneNumberType |
| **Tablo Açıklaması** | Telefon türlerini tanımlayan tablo. Cep, ev, iş telefonu gibi kategorileri yönetir. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| Name | nvarchar(50) | Telefon Türü Adı | Telefon türü adı |
| PhoneNumberTypeID | int | Telefon Türü Numarası | Telefon türü kimliği |

---

### Password Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Person |
| **Tablo Adı** | Password |
| **Tablo Açıklaması** | Şifre bilgilerini güvenli şekilde saklayan tablo. Hash ve salt değerleri ile şifre güvenliğini sağlar. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| BusinessEntityID | int | İş Varlığı Numarası | İş varlığı kimliği |
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| PasswordHash | varchar(128) | Şifre Hash Değeri | Şifre hash değeri |
| PasswordSalt | varchar(10) | Şifre Salt Değeri | Şifre salt değeri |
| rowguid | uniqueidentifier | Satır Tanımlayıcısı | Satır GUID |

---

### BusinessEntityAddress Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Person |
| **Tablo Adı** | BusinessEntityAddress |
| **Tablo Açıklaması** | İş varlıkları ve adresler arasındaki ilişkileri yöneten tablo. Hangi adresin hangi iş varlığına ait olduğunu belirler. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| AddressID | int | Adres Numarası | Adres kimliği |
| AddressTypeID | int | Adres Türü Numarası | Adres türü kimliği |
| BusinessEntityID | int | İş Varlığı Numarası | İş varlığı kimliği |
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| rowguid | uniqueidentifier | Satır Tanımlayıcısı | Satır GUID |

---

### BusinessEntityContact Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Person |
| **Tablo Adı** | BusinessEntityContact |
| **Tablo Açıklaması** | İş varlıkları ve iletişim kişileri arasındaki ilişkileri yöneten tablo. İş birimlerinin iletişim sorumluları bilgilerini saklar. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| BusinessEntityID | int | İş Varlığı Numarası | İş varlığı kimliği |
| ContactTypeID | int | İletişim Türü Numarası | İletişim türü kimliği |
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| PersonID | int | Kişi Numarası | Kişi kimliği |
| rowguid | uniqueidentifier | Satır Tanımlayıcısı | Satır GUID |

---

## Purchasing Şeması - Satın Alma & Tedarikçiler

### Vendor Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Purchasing |
| **Tablo Adı** | Vendor |
| **Tablo Açıklaması** | Tedarikçi bilgilerini saklayan tablo. **BusinessEntity ilişkisi:** BusinessEntityID → Person.BusinessEntity (tedarikçi adresleri ve iletişim kişileri için). **Ürün tedariki:** ProductVendor tablosu ile hangi tedarikçiden hangi ürün alınabileceği belirlenir. **Satın alma siparişleri:** PurchaseOrderHeader.VendorID ile siparişlere bağlanır. **Kalite değerlendirmesi:** CreditRating (1-5 arası, 1=en iyi), PreferredVendorStatus=1 ise tercih edilen tedarikçi. ActiveFlag=0 ise pasif tedarikçi. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| AccountNumber | nvarchar(15) | Hesap Numarası | Hesap numarası |
| ActiveFlag | bit | Aktif Bayrak | Aktif durumu |
| BusinessEntityID | int | İş Varlığı Numarası | İş varlığı kimliği |
| CreditRating | tinyint | Kredi Notu | Kredi notu |
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| Name | nvarchar(50) | Tedarikçi Adı | Tedarikçi adı |
| PreferredVendorStatus | bit | Tercih Edilen Tedarikçi | Tercih edilen tedarikçi durumu |
| PurchasingWebServiceURL | nvarchar(MAX) | Satın Alma Web Servis URL | Satın alma web servis URL'si |

---

### ShipMethod Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Purchasing |
| **Tablo Adı** | ShipMethod |
| **Tablo Açıklaması** | Kargo yöntemlerini tanımlayan tablo. Nakliye seçenekleri, maliyetleri ve oranlarını yönetir. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| Name | nvarchar(50) | Kargo Yöntemi Adı | Kargo yöntemi adı |
| rowguid | uniqueidentifier | Satır Tanımlayıcısı | Satır GUID |
| ShipBase | money | Kargo Taban Ücreti | Kargo taban ücreti |
| ShipMethodID | int | Kargo Yöntemi Numarası | Kargo yöntemi kimliği |
| ShipRate | money | Kargo Oranı | Kargo oranı |

---

### ProductVendor Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Purchasing |
| **Tablo Adı** | ProductVendor |
| **Tablo Açıklaması** | Ürün-tedarikçi ilişkilerini yöneten tablo. Tedarik süreleri, fiyatlar ve sipariş miktarları bilgilerini saklar. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| AverageLeadTime | int | Ortalama Tedarik Süresi | Ortalama tedarik süresi |
| BusinessEntityID | int | İş Varlığı Numarası | İş varlığı kimliği |
| LastReceiptCost | money | Son Alım Maliyeti | Son alım maliyeti |
| LastReceiptDate | datetime | Son Alım Tarihi | Son alım tarihi |
| MaxOrderQty | int | Maksimum Sipariş Miktarı | Maksimum sipariş miktarı |
| MinOrderQty | int | Minimum Sipariş Miktarı | Minimum sipariş miktarı |
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| OnOrderQty | int | Siparişteki Miktar | Siparişteki miktar |
| ProductID | int | Ürün Numarası | Ürün kimliği |
| StandardPrice | money | Standart Fiyat | Standart fiyat |
| UnitMeasureCode | nchar(3) | Ölçü Birimi Kodu | Ölçü birimi kodu |

---

### PurchaseOrderHeader Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Purchasing |
| **Tablo Adı** | PurchaseOrderHeader |
| **Tablo Açıklaması** | Tedarikçilere verilen satın alma siparişlerinin ana bilgilerini saklayan tablo. **Tedarikçi ilişkisi:** VendorID → Vendor. **Sipariş veren:** EmployeeID → HumanResources.Employee (satın alma sorumlusu). **Kargo yöntemi:** ShipMethodID → ShipMethod. **Durum (Status):** 1=Beklemede, 2=Onaylandı, 3=Reddedildi, 4=Tamamlandı. **Detay satırları:** PurchaseOrderDetail tablosunda sipariş edilen ürünler saklanır. Tedarikçi performansı, maliyet analizi ve tedarik zinciri raporları için kullanılır. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| EmployeeID | int | Çalışan Numarası | Çalışan kimliği |
| Freight | money | Nakliye Ücreti | Nakliye ücreti |
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| OrderDate | datetime | Sipariş Tarihi | Sipariş tarihi |
| PurchaseOrderID | int | Satın Alma Siparişi Numarası | Satın alma sipariş kimliği |
| RevisionNumber | tinyint | Revizyon Numarası | Revizyon numarası |
| ShipDate | datetime | Sevkiyat Tarihi | Sevkiyat tarihi |
| ShipMethodID | int | Kargo Yöntemi Numarası | Kargo yöntemi kimliği |
| Status | tinyint | Durum | Durum |
| SubTotal | money | Ara Toplam | Ara toplam |
| TaxAmt | money | Vergi Tutarı | Vergi tutarı |
| TotalDue | money | Toplam Borç | Toplam borç |
| VendorID | int | Tedarikçi Numarası | Tedarikçi kimliği |

---

### PurchaseOrderDetail Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | Purchasing |
| **Tablo Adı** | PurchaseOrderDetail |
| **Tablo Açıklaması** | Satın alma siparişlerinin kalem detaylarını saklayan tablo. **Ana tablo ilişkisi:** PurchaseOrderID → PurchaseOrderHeader. **Ürün ilişkisi:** ProductID → Production.Product. **Teslimat takibi:** OrderQty (sipariş edilen), ReceivedQty (teslim alınan), RejectedQty (kalite kontrolde reddedilen), StockedQty (stoka giren) miktarları karşılaştırılarak tedarikçi performansı ölçülür. **Tutar hesaplaması:** LineTotal = OrderQty × UnitPrice. DueDate ile beklenen teslimat tarihi izlenir. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| DueDate | datetime | Teslim Tarihi | Teslim tarihi |
| LineTotal | numeric(19,4) | Satır Toplamı | Satır toplamı |
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| OrderQty | smallint | Sipariş Miktarı | Sipariş miktarı |
| ProductID | int | Ürün Numarası | Ürün kimliği |
| PurchaseOrderDetailID | int | Satın Alma Sipariş Detay Numarası | Satın alma sipariş detay kimliği |
| PurchaseOrderID | int | Satın Alma Siparişi Numarası | Satın alma sipariş kimliği |
| ReceivedQty | smallint | Alınan Miktar | Alınan miktar |
| RejectedQty | smallint | Reddedilen Miktar | Reddedilen miktar |
| StockedQty | smallint | Stoklanmış Miktar | Stoklanmış miktar |
| UnitPrice | money | Birim Fiyat | Birim fiyat |

---

## HumanResources Şeması - İnsan Kaynakları & Çalışanlar

### Department Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | HumanResources |
| **Tablo Adı** | Department |
| **Tablo Açıklaması** | Departman bilgilerini saklayan tablo. **Departman grupları (GroupName):** Executive General and Administration, Inventory Management, Manufacturing, Quality Assurance, Research and Development, Sales and Marketing. **Örnek departmanlar:** Engineering, Tool Design, Sales, Marketing, Purchasing, Research and Development, Production, Human Resources, Finance, Information Services, Document Control, Quality Assurance, Facilities and Maintenance, Shipping and Receiving, Executive. **Çalışan ataması:** EmployeeDepartmentHistory tablosu ile çalışanların departman geçmişi izlenir. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| DepartmentID | smallint | Departman Numarası | Departman kimliği |
| GroupName | nvarchar(50) | Grup Adı | Grup adı |
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| Name | nvarchar(50) | Departman Adı | Departman adı |

---

### Employee Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | HumanResources |
| **Tablo Adı** | Employee |
| **Tablo Açıklaması** | Çalışan iş bilgilerini saklayan tablo. **Person tablosuyla ilişki:** BusinessEntityID → Person.Person (ad, soyad, iletişim bilgileri Person'da saklanır). **Satış temsilcileri:** Bir çalışan aynı zamanda satış temsilcisi ise Sales.SalesPerson tablosunda da kaydı olur. **Organizasyon hiyerarşisi:** OrganizationNode (hierarchyid) ile yönetici-çalışan ilişkisi tanımlanır. **Departman atama:** EmployeeDepartmentHistory ile çalışanın hangi departmanda çalıştığı izlenir. **Ücret geçmişi:** EmployeePayHistory ile maaş değişiklikleri takip edilir. Gender: M=Erkek, F=Kadın. MaritalStatus: S=Bekar, M=Evli. SalariedFlag: 1=Maaşlı, 0=Saatlik ücretli. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| BirthDate | datetime | Doğum Tarihi | Doğum tarihi |
| BusinessEntityID | int | İş Varlığı Numarası | İş varlığı kimliği |
| CurrentFlag | bit | Güncel Bayrak | Güncel durum |
| Gender | nchar(1) | Cinsiyet | Cinsiyet |
| HireDate | datetime | İşe Alım Tarihi | İşe alım tarihi |
| JobTitle | nvarchar(50) | İş Ünvanı | İş unvanı |
| LoginID | nvarchar(256) | Giriş Numarası | Giriş kimliği |
| MaritalStatus | nchar(1) | Medeni Durum | Medeni durum |
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| NationalIDNumber | nvarchar(15) | Ulusal Kimlik Numarası | Ulusal kimlik numarası |
| OrganizationLevel | smallint | Organizasyon Seviyesi | Organizasyon seviyesi |
| OrganizationNode | hierarchyid | Organizasyon Düğümü | Organizasyon düğümü |
| rowguid | uniqueidentifier | Satır Tanımlayıcısı | Satır GUID |
| SalariedFlag | bit | Maaşlı Bayrak | Maaşlı çalışan durumu |
| SickLeaveHours | smallint | Hastalık İzni Saatleri | Hastalık izni saatleri |
| VacationHours | smallint | Tatil Saatleri | Tatil saatleri |

---

### EmployeeDepartmentHistory Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | HumanResources |
| **Tablo Adı** | EmployeeDepartmentHistory |
| **Tablo Açıklaması** | Çalışan departman geçmişini saklayan tablo. Departman değişiklikleri ve görev sürelerini takip eder. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| BusinessEntityID | int | İş Varlığı Numarası | İş varlığı kimliği |
| DepartmentID | smallint | Departman Numarası | Departman kimliği |
| EndDate | datetime | Bitiş Tarihi | Bitiş tarihi |
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| ShiftID | tinyint | Vardiya Numarası | Vardiya kimliği |
| StartDate | datetime | Başlangıç Tarihi | Başlangıç tarihi |

---

### EmployeePayHistory Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | HumanResources |
| **Tablo Adı** | EmployeePayHistory |
| **Tablo Açıklaması** | Çalışan maaş geçmişini saklayan tablo. Ücret değişiklikleri ve ödeme sıklığını takip eder. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| BusinessEntityID | int | İş Varlığı Numarası | İş varlığı kimliği |
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| PayFrequency | tinyint | Ödeme Sıklığı | Ödeme sıklığı |
| Rate | money | Ücret Oranı | Ücret oranı |
| RateChangeDate | datetime | Ücret Değişiklik Tarihi | Ücret değişiklik tarihi |

---

### Shift Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | HumanResources |
| **Tablo Adı** | Shift |
| **Tablo Açıklaması** | Vardiya bilgilerini saklayan tablo. Çalışma saatleri ve vardiya türlerini yönetir. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| EndTime | time(7) | Bitiş Saati | Bitiş saati |
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| Name | nvarchar(50) | Vardiya Adı | Vardiya adı |
| ShiftID | tinyint | Vardiya Numarası | Vardiya kimliği |
| StartTime | time(7) | Başlangıç Saati | Başlangıç saati |

---

### JobCandidate Tablosu

| Özellik | Değer |
|---------|-------|
| **Şema Adı** | HumanResources |
| **Tablo Adı** | JobCandidate |
| **Tablo Açıklaması** | İş adaylarını saklayan tablo. Başvuru sahipleri ve özgeçmiş bilgilerini yönetir. |

| COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
|---|---|---|---|
| BusinessEntityID | int | İş Varlığı Numarası | İş varlığı kimliği |
| JobCandidateID | int | İş Adayı Numarası | İş adayı kimliği |
| ModifiedDate | datetime | Değiştirilme Tarihi | Değiştirilme tarihi |
| Resume | xml | Özgeçmiş | Özgeçmiş |

---


## 🔗 Foreign Key İlişkileri (JOIN Paths)

> **ÖNEMLİ**: Bu bölüm tablolar arası ilişkileri tanımlar. Neo4j Schema Catalog'da `REFERENCES` ilişkilerini oluşturmak ve JOIN path'lerini bulmak için kullanılır.

### İlişki Formatı

| Alan | Açıklama |
|------|----------|
| **Şema_Adi** | FK'nın bulunduğu tablonun şeması |
| **Tablo** | FK'nın bulunduğu tablo |
| **Tablo_PK** | Tablonun Primary Key kolonları |
| **FK_Kolon** | Foreign Key kolonu |
| **Referans_Şema** | Referans verilen tablonun şeması |
| **Referans_Tablo** | Referans verilen tablo |
| **Referans_Kolon** | Referans verilen kolon |

---

### HumanResources Şeması İlişkileri

| Tablo | Tablo_PK | FK_Kolon | Referans_Şema | Referans_Tablo | Referans_Kolon |
|-------|----------|----------|----------------|-----------------|-----------------|
| Employee | BusinessEntityID | BusinessEntityID | Person | Person | BusinessEntityID |
| EmployeeDepartmentHistory | BusinessEntityID, DepartmentID, ShiftID, StartDate | BusinessEntityID | HumanResources | Employee | BusinessEntityID |
| EmployeeDepartmentHistory | BusinessEntityID, DepartmentID, ShiftID, StartDate | DepartmentID | HumanResources | Department | DepartmentID |
| EmployeeDepartmentHistory | BusinessEntityID, DepartmentID, ShiftID, StartDate | ShiftID | HumanResources | Shift | ShiftID |
| EmployeePayHistory | BusinessEntityID, RateChangeDate | BusinessEntityID | HumanResources | Employee | BusinessEntityID |
| JobCandidate | JobCandidateID | BusinessEntityID | HumanResources | Employee | BusinessEntityID |

---

### Person Şeması İlişkileri

| Tablo | Tablo_PK | FK_Kolon | Referans_Şema | Referans_Tablo | Referans_Kolon |
|-------|----------|----------|----------------|-----------------|-----------------|
| Address | AddressID | StateProvinceID | Person | StateProvince | StateProvinceID |
| BusinessEntityAddress | BusinessEntityID, AddressID, AddressTypeID | AddressID | Person | Address | AddressID |
| BusinessEntityAddress | BusinessEntityID, AddressID, AddressTypeID | AddressTypeID | Person | AddressType | AddressTypeID |
| BusinessEntityAddress | BusinessEntityID, AddressID, AddressTypeID | BusinessEntityID | Person | BusinessEntity | BusinessEntityID |
| BusinessEntityContact | BusinessEntityID, PersonID, ContactTypeID | BusinessEntityID | Person | BusinessEntity | BusinessEntityID |
| BusinessEntityContact | BusinessEntityID, PersonID, ContactTypeID | ContactTypeID | Person | ContactType | ContactTypeID |
| BusinessEntityContact | BusinessEntityID, PersonID, ContactTypeID | PersonID | Person | Person | BusinessEntityID |
| EmailAddress | BusinessEntityID, EmailAddressID | BusinessEntityID | Person | Person | BusinessEntityID |
| Password | BusinessEntityID | BusinessEntityID | Person | Person | BusinessEntityID |
| Person | BusinessEntityID | BusinessEntityID | Person | BusinessEntity | BusinessEntityID |
| PersonPhone | BusinessEntityID, PhoneNumber, PhoneNumberTypeID | BusinessEntityID | Person | Person | BusinessEntityID |
| PersonPhone | BusinessEntityID, PhoneNumber, PhoneNumberTypeID | PhoneNumberTypeID | Person | PhoneNumberType | PhoneNumberTypeID |
| StateProvince | StateProvinceID | CountryRegionCode | Person | CountryRegion | CountryRegionCode |
| StateProvince | StateProvinceID | TerritoryID | Sales | SalesTerritory | TerritoryID |

---

### Production Şeması İlişkileri

| Tablo | Tablo_PK | FK_Kolon | Referans_Şema | Referans_Tablo | Referans_Kolon |
|-------|----------|----------|----------------|-----------------|-----------------|
| BillOfMaterials | BillOfMaterialsID | ComponentID | Production | Product | ProductID |
| BillOfMaterials | BillOfMaterialsID | ProductAssemblyID | Production | Product | ProductID |
| BillOfMaterials | BillOfMaterialsID | UnitMeasureCode | Production | UnitMeasure | UnitMeasureCode |
| Document | DocumentNode | Owner | HumanResources | Employee | BusinessEntityID |
| Product | ProductID | ProductModelID | Production | ProductModel | ProductModelID |
| Product | ProductID | ProductSubcategoryID | Production | ProductSubcategory | ProductSubcategoryID |
| Product | ProductID | SizeUnitMeasureCode | Production | UnitMeasure | UnitMeasureCode |
| Product | ProductID | WeightUnitMeasureCode | Production | UnitMeasure | UnitMeasureCode |
| ProductCostHistory | ProductID, StartDate | ProductID | Production | Product | ProductID |
| ProductDocument | ProductID, DocumentNode | DocumentNode | Production | Document | DocumentNode |
| ProductDocument | ProductID, DocumentNode | ProductID | Production | Product | ProductID |
| ProductInventory | ProductID, LocationID | LocationID | Production | Location | LocationID |
| ProductInventory | ProductID, LocationID | ProductID | Production | Product | ProductID |
| ProductListPriceHistory | ProductID, StartDate | ProductID | Production | Product | ProductID |
| ProductModelIllustration | ProductModelID, IllustrationID | IllustrationID | Production | Illustration | IllustrationID |
| ProductModelIllustration | ProductModelID, IllustrationID | ProductModelID | Production | ProductModel | ProductModelID |
| ProductModelProductDescriptionCulture | ProductModelID, ProductDescriptionID, CultureID | CultureID | Production | Culture | CultureID |
| ProductModelProductDescriptionCulture | ProductModelID, ProductDescriptionID, CultureID | ProductDescriptionID | Production | ProductDescription | ProductDescriptionID |
| ProductModelProductDescriptionCulture | ProductModelID, ProductDescriptionID, CultureID | ProductModelID | Production | ProductModel | ProductModelID |
| ProductProductPhoto | ProductID, ProductPhotoID | ProductID | Production | Product | ProductID |
| ProductProductPhoto | ProductID, ProductPhotoID | ProductPhotoID | Production | ProductPhoto | ProductPhotoID |
| ProductReview | ProductReviewID | ProductID | Production | Product | ProductID |
| ProductSubcategory | ProductSubcategoryID | ProductCategoryID | Production | ProductCategory | ProductCategoryID |
| TransactionHistory | TransactionID | ProductID | Production | Product | ProductID |
| WorkOrder | WorkOrderID | ProductID | Production | Product | ProductID |
| WorkOrder | WorkOrderID | ScrapReasonID | Production | ScrapReason | ScrapReasonID |
| WorkOrderRouting | WorkOrderID, ProductID, OperationSequence | LocationID | Production | Location | LocationID |
| WorkOrderRouting | WorkOrderID, ProductID, OperationSequence | WorkOrderID | Production | WorkOrder | WorkOrderID |

---

### Purchasing Şeması İlişkileri

| Tablo | Tablo_PK | FK_Kolon | Referans_Şema | Referans_Tablo | Referans_Kolon |
|-------|----------|----------|----------------|-----------------|-----------------|
| ProductVendor | ProductID, BusinessEntityID | BusinessEntityID | Purchasing | Vendor | BusinessEntityID |
| ProductVendor | ProductID, BusinessEntityID | ProductID | Production | Product | ProductID |
| ProductVendor | ProductID, BusinessEntityID | UnitMeasureCode | Production | UnitMeasure | UnitMeasureCode |
| PurchaseOrderDetail | PurchaseOrderID, PurchaseOrderDetailID | ProductID | Production | Product | ProductID |
| PurchaseOrderDetail | PurchaseOrderID, PurchaseOrderDetailID | PurchaseOrderID | Purchasing | PurchaseOrderHeader | PurchaseOrderID |
| PurchaseOrderHeader | PurchaseOrderID | EmployeeID | HumanResources | Employee | BusinessEntityID |
| PurchaseOrderHeader | PurchaseOrderID | ShipMethodID | Purchasing | ShipMethod | ShipMethodID |
| PurchaseOrderHeader | PurchaseOrderID | VendorID | Purchasing | Vendor | BusinessEntityID |
| Vendor | BusinessEntityID | BusinessEntityID | Person | BusinessEntity | BusinessEntityID |

---

### Sales Şeması İlişkileri

| Tablo | Tablo_PK | FK_Kolon | Referans_Şema | Referans_Tablo | Referans_Kolon |
|-------|----------|----------|----------------|-----------------|-----------------|
| CountryRegionCurrency | CountryRegionCode, CurrencyCode | CountryRegionCode | Person | CountryRegion | CountryRegionCode |
| CountryRegionCurrency | CountryRegionCode, CurrencyCode | CurrencyCode | Sales | Currency | CurrencyCode |
| CurrencyRate | CurrencyRateID | FromCurrencyCode | Sales | Currency | CurrencyCode |
| CurrencyRate | CurrencyRateID | ToCurrencyCode | Sales | Currency | CurrencyCode |
| Customer | CustomerID | PersonID | Person | Person | BusinessEntityID |
| Customer | CustomerID | StoreID | Sales | Store | BusinessEntityID |
| Customer | CustomerID | TerritoryID | Sales | SalesTerritory | TerritoryID |
| PersonCreditCard | BusinessEntityID, CreditCardID | BusinessEntityID | Person | Person | BusinessEntityID |
| PersonCreditCard | BusinessEntityID, CreditCardID | CreditCardID | Sales | CreditCard | CreditCardID |
| SalesOrderDetail | SalesOrderID, SalesOrderDetailID | ProductID | Sales | SpecialOfferProduct | ProductID |
| SalesOrderDetail | SalesOrderID, SalesOrderDetailID | SalesOrderID | Sales | SalesOrderHeader | SalesOrderID |
| SalesOrderDetail | SalesOrderID, SalesOrderDetailID | SpecialOfferID | Sales | SpecialOfferProduct | SpecialOfferID |
| SalesOrderHeader | SalesOrderID | BillToAddressID | Person | Address | AddressID |
| SalesOrderHeader | SalesOrderID | CreditCardID | Sales | CreditCard | CreditCardID |
| SalesOrderHeader | SalesOrderID | CurrencyRateID | Sales | CurrencyRate | CurrencyRateID |
| SalesOrderHeader | SalesOrderID | CustomerID | Sales | Customer | CustomerID |
| SalesOrderHeader | SalesOrderID | SalesPersonID | Sales | SalesPerson | BusinessEntityID |
| SalesOrderHeader | SalesOrderID | ShipMethodID | Purchasing | ShipMethod | ShipMethodID |
| SalesOrderHeader | SalesOrderID | ShipToAddressID | Person | Address | AddressID |
| SalesOrderHeader | SalesOrderID | TerritoryID | Sales | SalesTerritory | TerritoryID |
| SalesOrderHeaderSalesReason | SalesOrderID, SalesReasonID | SalesOrderID | Sales | SalesOrderHeader | SalesOrderID |
| SalesOrderHeaderSalesReason | SalesOrderID, SalesReasonID | SalesReasonID | Sales | SalesReason | SalesReasonID |
| SalesPerson | BusinessEntityID | BusinessEntityID | HumanResources | Employee | BusinessEntityID |
| SalesPerson | BusinessEntityID | TerritoryID | Sales | SalesTerritory | TerritoryID |
| SalesPersonQuotaHistory | BusinessEntityID, QuotaDate | BusinessEntityID | Sales | SalesPerson | BusinessEntityID |
| SalesTaxRate | SalesTaxRateID | StateProvinceID | Person | StateProvince | StateProvinceID |
| SalesTerritory | TerritoryID | CountryRegionCode | Person | CountryRegion | CountryRegionCode |
| SalesTerritoryHistory | BusinessEntityID, TerritoryID, StartDate | BusinessEntityID | Sales | SalesPerson | BusinessEntityID |
| SalesTerritoryHistory | BusinessEntityID, TerritoryID, StartDate | TerritoryID | Sales | SalesTerritory | TerritoryID |
| ShoppingCartItem | ShoppingCartItemID | ProductID | Production | Product | ProductID |
| SpecialOfferProduct | SpecialOfferID, ProductID | ProductID | Production | Product | ProductID |
| SpecialOfferProduct | SpecialOfferID, ProductID | SpecialOfferID | Sales | SpecialOffer | SpecialOfferID |
| Store | BusinessEntityID | BusinessEntityID | Person | BusinessEntity | BusinessEntityID |
| Store | BusinessEntityID | SalesPersonID | Sales | SalesPerson | BusinessEntityID |

---

## 📊 İlişki Özeti ve İstatistikleri

### Şema Bazlı İlişki Sayıları

| Şema | Tablo Sayısı | FK İlişki Sayısı | Referans Aldığı Şemalar |
|------|--------------|------------------|-------------------------|
| HumanResources | 6 | 6 | Person, HumanResources |
| Person | 13 | 14 | Person, Sales |
| Production | 21 | 29 | Production, HumanResources |
| Purchasing | 5 | 9 | Purchasing, Production, HumanResources, Person |
| Sales | 21 | 36 | Sales, Person, Production, Purchasing, HumanResources |

### En Çok Referans Verilen Tablolar (Hub Tables)

| Tablo | Referans Sayısı | Açıklama |
|-------|-----------------|----------|
| Production.Product | 15 | Merkez ürün tablosu |
| Person.BusinessEntity | 6 | Tüm iş varlıklarının ana tablosu |
| Person.Person | 6 | Kişi bilgileri merkezi |
| HumanResources.Employee | 5 | Çalışan bilgileri |
| Sales.SalesTerritory | 5 | Satış bölgeleri |
| Sales.SalesOrderHeader | 4 | Sipariş başlıkları |
| Production.ProductModel | 3 | Ürün modelleri |
| Sales.SalesPerson | 3 | Satış temsilcileri |
| Sales.Customer | 2 | Müşteri bilgileri |
| Person.Address | 2 | Adres bilgileri |

### Önemli JOIN Path Örnekleri

#### Satış → Müşteri → Kişi Bilgileri
```
Sales.SalesOrderHeader 
  → Sales.Customer (CustomerID)
    → Person.Person (PersonID → BusinessEntityID)
```

#### Sipariş → Ürün → Kategori
```
Sales.SalesOrderDetail 
  → Production.Product (ProductID)
    → Production.ProductSubcategory (ProductSubcategoryID)
      → Production.ProductCategory (ProductCategoryID)
```

#### Satış Temsilcisi → Çalışan → Kişi
```
Sales.SalesPerson 
  → HumanResources.Employee (BusinessEntityID)
    → Person.Person (BusinessEntityID)
```

#### Sipariş → Bölge → Ülke
```
Sales.SalesOrderHeader
  → Sales.SalesTerritory (TerritoryID)
    → Person.CountryRegion (CountryRegionCode)
```

---

## Son

Bu katalog AdventureWorks2022 veritabanının tüm tablolarını, görünümlerini ve Foreign Key ilişkilerini içermektedir. Her tablo için şema adı, tablo adı, tablo açıklaması ve tüm kolonlar ile Türkçe alias'lar ve detaylı açıklamalar sunulmuştur. FK ilişkileri bölümü, Neo4j Schema Catalog'da REFERENCES ilişkilerini oluşturmak ve JOIN path'lerini otomatik bulmak için kullanılır.