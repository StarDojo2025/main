#!/usr/bin/env python
# encoding: utf-8
from setuptools import setup, find_packages

setup(
    name="stardojo",
    version="0.1",
    packages=find_packages(),
    include_package_data=True,  
    package_data={
        '': ['res/*', 'conf/*'], 
    },
    install_requires=[
    ],
)


