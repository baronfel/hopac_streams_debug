to run:

AOT your mono install (from the /Library/Frameworks/Mono.framework/Versions/Current/lib/mono dir):
```
for i in `find gac -name '*.dll'` */mscorlib.dll; do sudo mono --aot $i; done
```

compile/aot the app:
```
./build_and_aot.sh
```

run the app from instruments, adding the `Time Profiler` and `Allocations` instruments.  You'll have to run the `mono` executable with args pointing to your app