all install:
	make -C src $@
	make -C login $@
	make -C util $@

clean:
	make -C src $@
	make -C login $@
	make -C util $@
	make -C doc $@

docs:
	make -C doc all

export:
	rm -rf /mnt/misc/sunuo /mnt/misc/runuo/SunUO.exe
	svn export . /mnt/misc/sunuo

release: VERSION := $(shell perl -ne 'print "$$1\n" if /^sunuo \((.*?)\)/' debian/changelog |head -1)
release: all docs
	rm -rf /tmp/sunuo
	mkdir -p /tmp/sunuo
	svn export . /tmp/sunuo/sunuo-$(VERSION)
	cd /tmp/sunuo && fakeroot zip -qr sunuo-$(VERSION).zip sunuo-$(VERSION)
	cd /tmp/sunuo && fakeroot tar cjf sunuo-$(VERSION).tar.bz2 sunuo-$(VERSION)
	mkdir -p /tmp/sunuo/sunuo-$(VERSION)-bin
	cp AUTHORS COPYING NEWS README doc/sunuo.html /tmp/sunuo/sunuo-$(VERSION)-bin
	cp debian/changelog /tmp/sunuo/sunuo-$(VERSION)-bin/changelog
	cp src/SunUO.exe util/UOGQuery.exe /tmp/sunuo/sunuo-$(VERSION)-bin
	cd /tmp/sunuo && fakeroot zip -qr sunuo-$(VERSION)-bin.zip sunuo-$(VERSION)-bin

upload: docs
	scp README NEWS debian/changelog doc/sunuo.html max@swift:/var/www/gzipped/download/sunuo/doc/
	ssh max@swift chmod a+rX -R /var/www/gzipped/download/sunuo/doc/
