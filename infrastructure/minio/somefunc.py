import typing


def somefunc(l: None | typing.List[str] = None):
    if l is None:
        l = []

    l.append("foo")
    return l



print(somefunc()) # ['foo']
print(somefunc())
