# AnyOTPMover
AnyOTP programını sms onayı olmadan başka bir bilgisayara taşımak için gerekli verileri aktaran ve çalışması için gerekli yamaları uygulayan yardımcı program.

# Kullanım
* Taşımak istediğiniz bilgisayarda AnyOTP programının yüklü ve çalışır halde olması gerekmektedir.  
* Taşımak istediğiniz bilgisayarda CopyOTP.exe yi yönetici olarak çalıştırın ve oluşan SpoofInfo.txt ve OtpInfo.reg dosyalarını alın.  
* Taşıdığınız bilgisayarda OtpInfo.reg dosyasını bir kereliğine çalıştırın ve gerekli kayıt defteri girdilerini ekleyin.  
* Taşıdığınız bilgisayarda AnyOTP'nin kurulu olduğu klasöre (C:/ProgramFiles(x86)/AnyOTP) gidip StartOTP.exe, HookWMIC.dll, SpoofInfo.txt dosyalarını kopyalayın.  
* StartOTP.exe'yi yönetici olarak çalıştırın.
* AnyOTP'yi normal olarak başlatırsanız çalışmayacaktır bundan sonra sürekli StartOTP.exe ile açmanız gerekmektedir.

# SpoofInfo.txt Hakkında
* 1.Satır HDD/SSD bilgisi
* 2.Satır HDD/SSD imzası
* 3.Satır AnyOTP'nin bir süre sonra otomatik kapanmasını önleyen patchi aktif/deaktif etmek için 

3.Satırdaki veriye 0 yazarsanız patch deaktif / 1 yazarsanız patchi aktif etmiş olursunuz.
