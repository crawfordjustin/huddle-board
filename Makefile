# Huddle Board — build and verification
#
# The only two commands you need day to day:
#   make          build dist/ from source
#   make test     build, then run every check
PY ?= python3

.PHONY: all build check test icons print clean

all: build

## build — data -> dist/proto_data.json -> dist/{HuddleBoard.html, deploy/, zip}
build:
	$(PY) export_proto.py
	@test -f dist/deploy/icon-192.png || $(MAKE) icons
	$(PY) build_app.py

## check — legality/safety/vocabulary pass over the play library only (fast)
check:
	$(PY) check_plays.py

## test — the full suite. Builds first, because every check drives a real build.
test: build
	$(PY) tests/run_all.py

## icons — regenerate the app icons (rarely needed)
icons:
	$(PY) make_icons.py

## print — the paper playbook, field cards and rotation sheet
print:
	$(PY) print/render.py
	$(PY) print/cards.py
	$(PY) print/rotation.py
	$(PY) print/topdf.py
	$(PY) print/topdf2.py dist/print/cards.html dist/print/8U-Field-Cards.pdf landscape
	$(PY) print/topdf2.py dist/print/rotation.html dist/print/8U-Rotation-Sheet.pdf landscape

clean:
	rm -rf dist __pycache__ */__pycache__
